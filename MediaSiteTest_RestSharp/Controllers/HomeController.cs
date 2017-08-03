using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;

namespace MediaSiteTest_RestSharp.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            List<VideoInfo> vlist = VideoList("");
            return View(vlist);
            
        }

        [HttpPost]
        public ActionResult Index(string txtSearch)
        {
            List<VideoInfo> vlist = VideoList(txtSearch);
            return View(vlist);

                //< canvas id = "myCanvas" width = "150" height = "150" style = "border:1px solid" >
                 //  < a target = "_blank" href = @item.videoContentURL > < img src = @item.thumbNailUrl height = "150" width = "150" ></ a >< br > @item.Title < br >
                //</ canvas >

        }

        public List<VideoInfo> VideoList(string searchCriteria)
        {
            //this.view.StartWaiting();
            string baseUri = @"http://ms.barrowmedia.com/mediasite/api/v1";
            //string searchCriteria = "cancer";
            string folderId = "";
            var batchSize = 25;
            var startIndex = 0;// (this.view.PageNumber - 1) * 25;

            //http://ms.barrowmedia.com/mediasite/api/v1/Presentations?filter=(Title%20eq%20'cancer') or (Description%20eq%20'cancer') &%24select=full&%24orderby=Title&%24top=100&%24skip=0

            //RestClient client = new RestClient(new Uri(baseUri, "api/v1").ToString());
            RestClient client = new RestClient(new Uri(baseUri).ToString());
            client.Authenticator = new Auth("AppTest", "Apptest2017!", "afac25e0-b125-49eb-8587-5ce355394d6d");

            var request = new RestRequest("Presentations", Method.GET);

            StringBuilder paramBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(searchCriteria))
            {
                paramBuilder.AppendFormat(" Title eq '{0}'", searchCriteria);
            }

            if (!string.IsNullOrEmpty(folderId))
            {
                if (paramBuilder.Length > 0)
                {
                    paramBuilder.AppendFormat(" and ");
                }

                paramBuilder.AppendFormat("ParentFolderId eq '{0}'", folderId);
            }

            if (paramBuilder.Length > 0)
            {
                request.AddParameter("$filter", paramBuilder.ToString());
            }

            request.AddParameter("$select", "full");
            request.AddParameter("$orderby", "Title");
            request.AddParameter("$top", batchSize);
            request.AddParameter("$skip", startIndex);
            //var presentations = this.ClientManager.Client.Execute<GenericResponse<PresentationFullRepresentation>>(request);
            var presentations = client.Execute<GenericResponse<PresentationFullRepresentation>>(request);


            // if (presentations.Data.value != null)
            // {
            //this.view.FillPresentations(presentations.Data.value.Select(i => mapToModel(i)));

            //var jsonData = presentations.Data.value.Select(i => mapToModel(i));

            //paging info
            var jsonObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(presentations.Content);
            //var jsonObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(presentations.Data.value);
            var val = jsonObject["odata.count"] as JValue;
            int recordCnt = (int) val;
            TempData["Recordcount"] = val.ToString();
            var valueData = jsonObject["value"];

            List<VideoInfo> videoDetails1 = new List<VideoInfo>();

            for (int i = 0; i < recordCnt; i++)
            {
                try
                {
                    String authTkt = requestAuthTicket("AppTest", presentations.Data.value[i].Id.ToString(), 10000);
                    videoDetails1.Add(new VideoInfo() { Id = presentations.Data.value[i].Id.ToString(), Title = presentations.Data.value[i].Title.ToString(), thumbNailUrl = presentations.Data.value[i].ThumbnailUrl + "?AuthTicket=" + authTkt, videoContentURL = @"http://ms.barrowmedia.com/Mediasite/Play/" + presentations.Data.value[i].Id + "?AuthTicket=" + authTkt });
                }
                catch (Exception ex)
                {
                }
            }
            /*
            List<VideoInfo> videoDetails = new List<VideoInfo>
            {
                new VideoInfo(){ Id = presentations.Data.value[0].Id.ToString(), Title = presentations.Data.value[0].Title.ToString(), thumbNailUrl = presentations.Data.value[0].ThumbnailUrl, videoContentURL=@"http://ms.barrowmedia.com/Mediasite/Play/"+presentations.Data.value[0].Id+"?AuthTicket=c4bec4fde1434e20b7766680d190d43d"},
                new VideoInfo(){ Id = presentations.Data.value[1].Id.ToString(), Title = presentations.Data.value[2].Title.ToString(), thumbNailUrl = presentations.Data.value[2].ThumbnailUrl,videoContentURL=@"http://ms.barrowmedia.com/Mediasite/Play/"+presentations.Data.value[1].Id+"?AuthTicket=c4bec4fde1434e20b7766680d190d43d"},
                new VideoInfo(){ Id = presentations.Data.value[3].Id.ToString(), Title = presentations.Data.value[3].Title.ToString(), thumbNailUrl = presentations.Data.value[3].ThumbnailUrl,videoContentURL=@"http://ms.barrowmedia.com/Mediasite/Play/"+presentations.Data.value[2].Id+"?AuthTicket=c4bec4fde1434e20b7766680d190d43d"}
            };
            */
            //this.view.NextPagesExists = int.Parse(val.Value.ToString()) > startIndex + batchSize;
            //this.view.PreviousPagesExists = this.view.PageNumber > 1;
            // }

            //this.view.EndWaiting();
            //return Json(jsonObject["value"], JsonRequestBehavior.AllowGet);
            //return Json(valueData, JsonRequestBehavior.AllowGet);
            return videoDetails1;
        }

        public String getBasicAuthHeader()
        {
            return "Authorization:Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", "AppTest", "Apptest2017!")));
        }

        private static DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AuthorizationTicket));
        public String requestAuthTicket(String ticketUsername, String resourceId, int minutesToLive)
        {

            AuthorizationTicket ticket = new AuthorizationTicket
            {
                Username = ticketUsername,
                ResourceId = resourceId,
                MinutesToLive = minutesToLive
            };
            //client.Authenticator = new Auth("AppTest", "Apptest2017!", "afac25e0-b125-49eb-8587-5ce355394d6d");
            var req = (HttpWebRequest)WebRequest.Create(@"http://ms.barrowmedia.com/mediasite/api/v1/AuthorizationTickets");
            req.Method = "POST";
            req.Headers.Add("sfapikey:" + "afac25e0-b125-49eb-8587-5ce355394d6d");
            req.ContentType = "application/json";

            req.Headers.Add(getBasicAuthHeader());

            Stream dataStream = req.GetRequestStream();
            serializer.WriteObject(dataStream, ticket);
            dataStream.Close();

            Stream responseStream = req.GetResponse().GetResponseStream();
            AuthorizationTicket ticketResponse = (AuthorizationTicket)serializer.ReadObject(responseStream);
            responseStream.Close();

            return ticketResponse.TicketId;
        }


    
        [HttpGet]
        public ViewResult PopulatePresentations()
        {
            //this.view.StartWaiting();
            string baseUri = @"http://ms.barrowmedia.com/mediasite/api/v1";
            string searchCriteria = "cancer";
            string folderId = "";
            var batchSize = 25;
            var startIndex = 0;// (this.view.PageNumber - 1) * 25;

            //RestClient client = new RestClient(new Uri(baseUri, "api/v1").ToString());
            RestClient client = new RestClient(new Uri(baseUri).ToString());
            client.Authenticator = new Auth("AppTest", "Apptest2017!", "afac25e0-b125-49eb-8587-5ce355394d6d");

            var request = new RestRequest("Presentations", Method.GET);

            StringBuilder paramBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(searchCriteria))
            {
                paramBuilder.AppendFormat("Title eq '{0}'", searchCriteria);
            }

            if (!string.IsNullOrEmpty(folderId))
            {
                if (paramBuilder.Length > 0)
                {
                    paramBuilder.AppendFormat(" and ");
                }

                paramBuilder.AppendFormat("ParentFolderId eq '{0}'", folderId);
            }

            if (paramBuilder.Length > 0)
            {
                request.AddParameter("$filter", paramBuilder.ToString());
            }

            request.AddParameter("$select", "full");
            request.AddParameter("$orderby", "Title");
            request.AddParameter("$top", batchSize);
            request.AddParameter("$skip", startIndex);
            //var presentations = this.ClientManager.Client.Execute<GenericResponse<PresentationFullRepresentation>>(request);
            var presentations = client.Execute<GenericResponse<PresentationFullRepresentation>>(request);


            // if (presentations.Data.value != null)
            // {
            //this.view.FillPresentations(presentations.Data.value.Select(i => mapToModel(i)));

            //var jsonData = presentations.Data.value.Select(i => mapToModel(i));

            //paging info
            var jsonObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(presentations.Content);
            //var jsonObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(presentations.Data.value);
            var val = jsonObject["odata.count"] as JValue;
            var valueData = jsonObject["value"];

            List<VideoInfo> videoDetails = new List<VideoInfo>
            {
                new VideoInfo(){ Id = presentations.Data.value[0].Id.ToString(), Title = presentations.Data.value[0].Title.ToString(), thumbNailUrl = presentations.Data.value[0].ThumbnailUrl, videoContentURL=@"http://ms.barrowmedia.com/Mediasite/Play/"+presentations.Data.value[0].Id},
                new VideoInfo(){ Id = presentations.Data.value[1].Id.ToString(), Title = presentations.Data.value[2].Title.ToString(), thumbNailUrl = presentations.Data.value[2].ThumbnailUrl, videoContentURL=@"http://ms.barrowmedia.com/Mediasite/Play/"+presentations.Data.value[1].Id},
                new VideoInfo(){ Id = presentations.Data.value[3].Id.ToString(), Title = presentations.Data.value[3].Title.ToString(), thumbNailUrl = presentations.Data.value[3].ThumbnailUrl, videoContentURL=@"http://ms.barrowmedia.com/Mediasite/Play/"+presentations.Data.value[2].Id}
            };

            //this.view.NextPagesExists = int.Parse(val.Value.ToString()) > startIndex + batchSize;
            //this.view.PreviousPagesExists = this.view.PageNumber > 1;
            // }

            //this.view.EndWaiting();
            //return Json(jsonObject["value"], JsonRequestBehavior.AllowGet);
            //return Json(valueData, JsonRequestBehavior.AllowGet);
            return View(videoDetails);
        }

        private VideoInfo mapToModel(PresentationFullRepresentation presentationDetails)
        {
            string name = presentationDetails.Title.Length > 100 ? string.Concat(presentationDetails.Title.Substring(0, 100), "...") : presentationDetails.Title;

            return new VideoInfo()
            {
                Id = presentationDetails.Id,
                Name = name,
                Title = presentationDetails.ParentFolderId,
                thumbNailUrl = presentationDetails.ThumbnailUrl
            };
        }


    }

    public class VideoInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string thumbNailUrl { get; set; }
        public string videoContentURL { get; set; }
    }
    public class PresentationDefaultRepresentation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }

        public PresentationDefaultRepresentation(PresentationDefaultRepresentation obj)
        {
            this.Id = obj.Id;
            this.Title = obj.Title;
            this.Status = obj.Status;
        }

        public PresentationDefaultRepresentation() { }
    }


    public class PresentationCardRepresentation : PresentationDefaultRepresentation
    {
        public string Description { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? RecordDateLocal { get; set; }
        public int Duration { get; set; }
        public int NumberOfViews { get; set; }
        public string Owner { get; set; }
        public string PrimaryPresenter { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsLive { get; set; }
        public DateTime? CreationDate { get; set; }

        public PresentationCardRepresentation() : base() { }

        public PresentationCardRepresentation(PresentationCardRepresentation obj)
            : base(obj)
        {
            this.Description = obj.Description;
            this.Status = obj.Status;
            this.PrimaryPresenter = obj.PrimaryPresenter;
            this.ThumbnailUrl = obj.ThumbnailUrl;
            this.RecordDate = obj.RecordDate;
            this.RecordDateLocal = obj.RecordDateLocal;
            this.Duration = obj.Duration;
            this.NumberOfViews = obj.NumberOfViews;
            this.Owner = obj.Owner;
            this.CreationDate = obj.CreationDate;
            this.IsLive = obj.IsLive;
        }
    }

    public class PresentationFullRepresentation : PresentationCardRepresentation
    {
        public string RootId { get; set; }
        public string PlayerId { get; set; }
        public string PresentationTemplateId { get; set; }
        public string AlternateName { get; set; }
        public string CopyrightNotice { get; set; }
        public int MaximumConnections { get; set; }
        public string PublishingPointName { get; set; }
        public bool IsUploadAutomatic { get; set; }
        public string TimeZone { get; set; }
        public bool PollsEnabled { get; set; }
        public bool ForumsEnabled { get; set; }
        public bool SharingEnabled { get; set; }
        public bool PlayerLocked { get; set; }
        public bool PollsInternal { get; set; }
        public bool Private { get; set; }
        public bool NotifyOnMetadataChanged { get; set; }
        public string ApprovalState { get; set; }
        public string ApprovalRequiredChangeTypes { get; set; }
        public int ContentRevision { get; set; }
        public string PollLink { get; set; }
        public string ParentFolderName { get; set; }
        public string ParentFolderId { get; set; }
        public DateTime? DisplayRecordDate { get; set; }

        public PresentationFullRepresentation() : base() { }
    }

    public class GenericResponse<T>
    {
        public List<T> value { get; set; }
    }

    public class Auth : IAuthenticator
    {
        private string userName, password, apiKey;

        public Auth(string userName, string password, string apiKey)
        {
            this.userName = userName;
            this.password = password;
            this.apiKey = apiKey;
        }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            var basicAuthHeaderValue = string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", this.userName, password))));
            request.AddHeader("Authorization", basicAuthHeaderValue);
            request.AddHeader("sfapikey", this.apiKey);
        }
    }


    [DataContract]
    class AuthorizationTicket
    {
        [DataMember(EmitDefaultValue = false)]
        public String TicketId { get; set; }

        [DataMember]
        public String Username { get; set; }

        [DataMember]
        public String ClientIpAddress { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Owner { get; set; }

        [DataMember]
        public String ResourceId { get; set; }

        [DataMember]
        public int MinutesToLive { get; set; }
    }

}