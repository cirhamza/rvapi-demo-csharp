using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using REALvisionApiLib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace
    REALvisionApiLib
{
    public class RealvisionApi
    {

        public static HttpClient RealvisionClient { get; set; }
        public string RootFolder { get; set; }
        public string DownloadsFolder { get; set; } //The folder where the downloaded files will be stored 
        public string AssetsFolder { get; set; }    //The folder where the files to slice are stored.
        public ApiSettings ApiSettings { get; set; } 
        public string Token { get; set; }
        public string TokenExpiresOn { get; set; }

        //The Slicing Configs
        public int CheckProgressInterval { get; set; } //The interval of time in which you check the progress of the slicing task (every 2 seconds for an example)


        private string saveFileTo { get; set; }

        public RealvisionApi(String rootFolder, string downloadsFolder)
        {
            this.RootFolder = rootFolder;
            this.DownloadsFolder = downloadsFolder;
            this.ApiSettings = this.getApiSettings(RootFolder);
            this.getToken(RootFolder).Wait();
            this.CheckProgressInterval = 2;
        }

        // ************************************************************************************* //
        // ******************************** API FUNCTIONS ************************************** //
        // ************************************************************************************* //

        public string getActivationStatus()
        {
            ApiRequest ApiRequest = new ApiRequest();
            return MakeRequest("POST", "GetActivationStatus", ApiRequest).Result.ResponseString;
        }

        public string UploadFile(string FileName, string FilePath)
        {
            byte[] fileToSliceBytes = File.ReadAllBytes(Path.Combine(FilePath,FileName));
            ApiRequest UploadFileRequest = new UploadApiRequest(FileName, fileToSliceBytes);

            return MakeRequest("POST", "UploadFile", UploadFileRequest).Result.ResponseString;

        }

        public string StartSlicingTask(NoConfigApiRequest stlFileConfigs, string fileId)
        {
            
            ApiRequest ApiRequest = new NoConfigApiRequest(stlFileConfigs.FileName, fileId, stlFileConfigs.SupportType, stlFileConfigs.PrinterModel, stlFileConfigs.ConfigPresetName, stlFileConfigs.Position, stlFileConfigs.Rotation, stlFileConfigs.Scale);

            return MakeRequest("POST", "StartSlicingTask", ApiRequest).Result.ResponseString;
        }
        public double GetProgress(String TaskId)
        {
            ApiRequest ApiRequest = new TaskApiRequest(TaskId);

            MakeRequestResponse resp = MakeRequest("POST", "GetProgress", ApiRequest).Result;
            Double.TryParse(resp.ResponseString, out double result);
            return result;
        }
        public string GetPrintingInformation(String TaskId)
        {
            ApiRequest ApiRequest = new TaskApiRequest(TaskId);
            return MakeRequest("POST", "GetPrintingInformation", ApiRequest).Result.ResponseString;
        }
        public void Downloadfile(String TaskId, string filePath)
        {
            double progress = GetProgress(TaskId);

            while (progress != 1 && progress != -1 && progress != 2 )
            {
                progress = GetProgress(TaskId);
                Console.WriteLine(progress);
            }
            if(progress == 1)
            {
                var result = MakeRequest("GET", "DownloadFile?taskid=" + TaskId, new ApiRequest(), true, filePath).Result;
            } else if ( progress == -1)
            {
                Console.WriteLine("Slicing file failed ... ");
            }

            else
            {
                Console.WriteLine("Encoutered error while downloading file ... ");
            }
        }

        // ************************************************************************************* //
        // ***************************** SUPPORT FUNCTIONS ************************************* //
        // ************************************************************************************* //
        public ApiSettings getApiSettings(String rootFolder)
        {
            JToken appSettings = JToken.Parse(File.ReadAllText(Path.Combine(rootFolder,"appsettings.json")));
            return ApiSettings = JsonConvert.DeserializeObject<ApiSettings>(JsonConvert.SerializeObject(appSettings["ApiSettings"]));
        }

        public List<NoConfigApiRequest> GetFilesToSlice(string stlFilesJsonListPath)
        {
            JToken stlFilesListString = JToken.Parse(File.ReadAllText(stlFilesJsonListPath));
            List<NoConfigApiRequest> stlFilesList = JsonConvert.DeserializeObject<List<NoConfigApiRequest>>(JsonConvert.SerializeObject(stlFilesListString));
            return stlFilesList;
        }
        public async Task<String> requestNewToken()
        {
            using (var client = new HttpClient())
            {

                MultipartFormDataContent multipart = new MultipartFormDataContent();

                StringContent grant_type = new StringContent("client_credentials");
                StringContent client_id = new StringContent(this.ApiSettings.ClientId);
                StringContent client_secret = new StringContent(this.ApiSettings.ClientSecret);
                StringContent resource = new StringContent("https://api.createitreal.com");

                multipart.Add(grant_type, "grant_type");
                multipart.Add(client_id, "client_id");
                multipart.Add(client_secret, "client_secret");
                multipart.Add(resource, "resource");

                HttpResponseMessage result = new HttpResponseMessage();

                try
                {
                        result = client.PostAsync(this.ApiSettings.AuthServerUrl, multipart).Result;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("Error while fetching token ... ");
                    throw e;
                }

                Console.WriteLine("*************************************************************************");
                string finalresult = await result.Content.ReadAsStringAsync();

                    
                if (!string.IsNullOrEmpty(JsonConvert.DeserializeObject<ApiJwtResponse>(finalresult).access_token))
                {
                    Console.WriteLine("Token successfully fetched ... ");
                }
                else
                {
                    throw new Exception("Error while fetching new token, please check your if your API credentials are valid and try again.");
                }

                return finalresult;
            }

        }

        public async Task<String> getToken(String RootFolder)
        {
            string tokenFile = "";
            ApiJwtResponse jwt = new ApiJwtResponse();

            try
            {
                tokenFile = File.ReadAllText(RootFolder + "/token.json");
                jwt = JsonConvert.DeserializeObject<ApiJwtResponse>(tokenFile);

                Console.WriteLine("*************************************************************************");
                Console.WriteLine("Valid token file.");
                Console.WriteLine("*************************************************************************");
            }
            catch
            {
                Console.WriteLine("*************************************************************************");
                Console.WriteLine("No token file found. requesting new token ...");
                Console.WriteLine("*************************************************************************");

                File.WriteAllText(RootFolder + "/token.json", await requestNewToken());
            }

            if (!string.IsNullOrEmpty(jwt.access_token))
            {
                this.TokenExpiresOn = jwt.expires_on;
                DateTime foo = DateTime.UtcNow;
                long unixTime = ((DateTimeOffset)foo).ToUnixTimeSeconds();

                bool newTokenNeeded = !(Double.Parse(this.TokenExpiresOn) - unixTime > 0);

                if (!newTokenNeeded)
                {
                    Console.WriteLine("*************************************************************************");
                    Console.WriteLine("Valid token file.");
                    Console.WriteLine("*************************************************************************");

                    this.Token = jwt.access_token;
                    this.TokenExpiresOn = jwt.expires_on;

                    return this.Token;
                } else
                {
                    Console.WriteLine("*************************************************************************");
                    Console.WriteLine("Available token no longer valid, requesting new token ... ");
                    Console.WriteLine("*************************************************************************");

                    File.WriteAllText(RootFolder + "/token.json", await requestNewToken());
                    this.Token = jwt.access_token;

                    return this.Token;
                }
            } else
            {
                Console.WriteLine("*************************************************************************");
                Console.WriteLine("Token file doesn't contain token, requesting new token ... ");
                Console.WriteLine("*************************************************************************");
                string newToken = await requestNewToken();
                File.WriteAllText(RootFolder + "/token.json", newToken);
                Console.WriteLine("TOKEN    ::::: " + newToken);
                return newToken;
            }
        }

        private string readHttpResponse(HttpWebResponse response)
        {
            System.IO.Stream responseStream = response.GetResponseStream();
            StreamReader responseReader = new StreamReader(responseStream);

            return responseReader.ReadToEnd();
        }

        private async void logResponse(HttpResponseMessage response, string serviceCall , bool isDownload, string filePath = "" )
        {

            Console.WriteLine();
            Console.WriteLine("*************************************************************************");
            Console.WriteLine("SERVICECALL                  :::: " + serviceCall);
            
            Console.WriteLine();
            Console.WriteLine("METHOD                       :::: " + response.RequestMessage.Method);
            Console.WriteLine("REQUEST_STATUS_CODE          :::: " + response.StatusCode);

            if( response.IsSuccessStatusCode && serviceCall == "UploadFile")
            {
                Console.WriteLine("--------------------");
                Console.WriteLine("RESPONSE                 :::: " + " The following code is the ID of your file which you should use to start a slicing task: ");

                FileIdResponse resObj = JsonConvert.DeserializeObject<FileIdResponse>(response.Content.ReadAsStringAsync().Result);

                Console.WriteLine(resObj.Result.FileId);
                Console.WriteLine("--------------------");
            }
            else if ( isDownload  && HttpStatusCode.OK == response.StatusCode )
            {
                Console.WriteLine("--------------------");
                Console.WriteLine("RESPONSE                 :::: " + " Please check the following folder for the downloaded FCode file: ");
                Console.WriteLine(filePath);
                Console.WriteLine("--------------------");

            }
            else
            {
                Console.WriteLine("RESPONSE                     :::: " + await response.Content.ReadAsStringAsync());
            }

            Console.WriteLine("*************************************************************************");
            Console.WriteLine();

        }

        private void SaveFile(String response, string fileName, string fileExtention, string customFolderPath = "" )
        {
            DateTime foo = DateTime.UtcNow;
            long utc = ((DateTimeOffset)foo).ToUnixTimeSeconds();
            string timeStamp = utc.ToString();

            //This checks if the downloads folder exists or not, in case it doesn't exist, it creates one
            System.IO.Directory.CreateDirectory(RootFolder + @"/downloads");

            string fullPath = this.DownloadsFolder + @"/" + fileName + "." + timeStamp + fileExtention;

            try
            {
                File.WriteAllText(fullPath, response);
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR WHILE SAVING FILE TO FILESYSTEM", ex);
            }
        }

        // ************************************************************************************* //
        //This function is used by all the API Functions to call the API 
        // ************************************************************************************* //

        public async Task<MakeRequestResponse> MakeRequest (String method, string serviceCall, ApiRequest ApiRequest, bool isDownload = false, string downloadFolderPath = "" )
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {

                    //Authentication & Authorization
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this.ApiSettings.ApiKey);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.Token);
                    
                    HttpResponseMessage result = new HttpResponseMessage();

                    //Making the request depending on which type of request( multipart/form-data or application/json )
                    //Only UploadFile uses multipart/form-data

                    if ( serviceCall == "UploadFile")
                    {
                        //Turn the ApiRequest into an UploadApiRequest
                        string tempString = JsonConvert.SerializeObject(ApiRequest);
                        UploadApiRequest apiReq = JsonConvert.DeserializeObject<UploadApiRequest>(tempString);
                        //The API main link ( ex: http://localhost:5001/REALvision/ ) 
                        client.BaseAddress = new Uri(this.ApiSettings.ApiUrl);
                        //Initializing the multipart Http request entity
                        MultipartFormDataContent multipartRequest = new MultipartFormDataContent();

                        //Adding details to the request like the filename and the file itself
                        HttpContent FileNameContent = new StringContent(apiReq.FileName);
                        HttpContent stlFileContent = new ByteArrayContent(apiReq.File);

                        multipartRequest.Add(FileNameContent, "FileName");
                        multipartRequest.Add(stlFileContent, "file", apiReq.FileName);

                        try
                        {
                            result = client.PostAsync("UploadFile", multipartRequest).Result;
                        }
                        catch (HttpRequestException e)
                        {
                            Console.WriteLine("Error while uploading file  ... ");
                            throw e;
                        }

                        Console.WriteLine("*************************************************************************");

                        //Logging the results of the request
                        this.logResponse(result, serviceCall, false);

                        if(result.IsSuccessStatusCode)
                        {
                            string uploadFileResponseString = await result.Content.ReadAsStringAsync();
                            FileIdResponse uploadFileResponse = JsonConvert.DeserializeObject<FileIdResponse>(uploadFileResponseString);
                            return new MakeRequestResponse(result.StatusCode,uploadFileResponse.Result.FileId);
                        }
                        else
                        {
                            string errorResponseString = await result.Content.ReadAsStringAsync();
                            //ApiErrorResponse errorResponse = JsonConvert.DeserializeObject<ApiErrorResponse>(errorResponseString);

                            return new MakeRequestResponse(result.StatusCode, errorResponseString);
                        }
                        
                    }
                    else
                    {
                        string downloadFileName = "";
                        //Turn the ApiRequest into an UploadApiRequest
                        string tempString = JsonConvert.SerializeObject(ApiRequest);
                        //We use the string for the HTTP request 
                        if(method == "GET"){
                            result = client.GetAsync(this.ApiSettings.ApiUrl + serviceCall).Result;
                        } else {
                            result = client.PostAsync(this.ApiSettings.ApiUrl + serviceCall, new StringContent(tempString, Encoding.UTF8, "application/json")).Result;
                        }

                        if (result.IsSuccessStatusCode)
                        {

                            //Reading the response as a string
                            var response = await result.Content.ReadAsStringAsync();

                            if (!isDownload)
                            {
                                this.logResponse(result, serviceCall, false);

                                switch (serviceCall)
                                {
                                    case "GetActivationStatus":
                                        ActivationStatusResponse resp = JsonConvert.DeserializeObject<ActivationStatusResponse>(response);
                                        return new MakeRequestResponse(result.StatusCode,resp.Result.Activated);
                                    case "StartSlicingTask":
                                        TaskIdResponse startSlicingTaskResponse = JsonConvert.DeserializeObject<TaskIdResponse>(response);
                                        return new MakeRequestResponse(result.StatusCode, startSlicingTaskResponse.Result.TaskId);
                                    case "GetProgress":
                                        ProgressResponse responseObject = JsonConvert.DeserializeObject<ProgressResponse>(response);
                                        return new MakeRequestResponse(result.StatusCode, responseObject.Result.Progress);
                                    case "GetPrintingInformation":
                                        PrintingInformationResponse printingInformationResponse = JsonConvert.DeserializeObject<PrintingInformationResponse>(response);
                                        return new MakeRequestResponse(result.StatusCode, JsonConvert.SerializeObject(printingInformationResponse.Result));
                                    default:
                                        return new MakeRequestResponse(result.StatusCode, "Service call could not be found.");
                                }
                            }
                            else
                            {
                                this.logResponse(result, serviceCall, isDownload, downloadFolderPath + downloadFileName);

                                downloadFileName = result.Content.Headers.ContentDisposition.FileName;
                                SaveFile(response, Path.GetFileNameWithoutExtension(downloadFileName), Path.GetExtension(downloadFileName), downloadFolderPath);

                                return new MakeRequestResponse(result.StatusCode, response);
                            }

                        }
                        else
                        {

                            string errorResponseString = await result.Content.ReadAsStringAsync();
                            ApiErrorResponse errorResponse = JsonConvert.DeserializeObject<ApiErrorResponse>(errorResponseString);

                            throw new Exception(errorResponse.Error.ErrorMessage);
                        }
                    }
                }
                catch (WebException e)
                {
                    throw e;
                }
            }
        }
    }
}
