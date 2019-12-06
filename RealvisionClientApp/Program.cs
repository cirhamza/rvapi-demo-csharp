 using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using REALvisionApiLib;
using REALvisionApiLib.Models;

namespace 
    DemoConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {

            // ********************************************************************************//
            //  Initializing the configurations
            // ********************************************************************************//

            string rootFolder = Directory.GetCurrentDirectory();

            if (rootFolder.Contains("bin"))
            {
                rootFolder = Path.Combine(Directory.GetCurrentDirectory() + "../../../../");
            }
            string downloadsFolder = Path.Combine(rootFolder, "downloads");
            string assetsFolder = Path.Combine(rootFolder, "assets");
            //Initializing a REALvision API instance using the library
            RealvisionApi rv = new REALvisionApiLib.RealvisionApi(rootFolder, downloadsFolder);
            //Getting the list of the STL files to be sliced whose information are stored in the "filesToSlice.json"
            List<NoConfigApiRequest> stlFilesList = rv.GetFilesToSlice(rootFolder + @"/filesToSlice.json");

            // ********************************************************************************//
            //  This is where we execute the whole slicing flow: 
            // ********************************************************************************//

            //Execute the slicing flow for every file in the stlFilesList
            foreach (NoConfigApiRequest stlFile in stlFilesList){
                ExecuteSlicingFlow(stlFile);
            }

            // ********************************************************************************//
            //  The whole slicing flow represented by a function "ExecuteSlicingFlow" that takes in the information of the file to slice 
            //  in the form of a NoConfigApiRequest class.
            // ********************************************************************************//

            void ExecuteSlicingFlow(NoConfigApiRequest stlFileToSlice)
            {

                Console.WriteLine("=====================================================================================================================");
                Console.WriteLine("Slicing first file : " + stlFileToSlice.FileName);
                Console.WriteLine("=====================================================================================================================");
                Console.WriteLine();
                Console.WriteLine();

                // ********************************************************************************//
                //  To call https://realvisiononline.azure-api.net/GetActivationStatus
                // ********************************************************************************//
                string activationStatus = rv.getActivationStatus();


                if ( activationStatus == "true")
                {
                    // ********************************************************************************//
                    //  To call https://realvisiononline.azure-api.net/UploadFile
                    // ********************************************************************************//

                    string fileId = rv.UploadFile(stlFileToSlice.FileName, assetsFolder);
                    
                    // ********************************************************************************//
                    //  To call https://realvisiononline.azure-api.net/StartSlicingTask
                    // ********************************************************************************//

                    string taskId = rv.StartSlicingTask(stlFileToSlice, fileId);

                    // ********************************************************************************//
                    //  To call https://realvisiononline.azure-api.net/GetProgress
                    // ********************************************************************************//

                    double progress = 0;

                    while (progress < 1 && progress != -1)
                    {
                        progress = rv.GetProgress(taskId);
                        Thread.Sleep(TimeSpan.FromSeconds(rv.CheckProgressInterval));
                    }

                    //// ********************************************************************************//
                    ////  To call https://realvisiononline.azure-api.net/GetPrintingInformation
                    //// ********************************************************************************//

                    rv.GetPrintingInformation(taskId);

                    //// ********************************************************************************//
                    ////  To call https://realvisiononline.azure-api.net/DownloadFile
                    //// ********************************************************************************//

                    ////Note: DownloadFile will first check the progress of the slicing process before downloading the file
                    ////      which is why you'll notice in the Console that GetProgress is executed a few times before DownloadFile is executed
                    
                    rv.Downloadfile(taskId, downloadsFolder);
                
                }
                else
                {
                    Console.WriteLine("The server needs to be activated for you to be able to slice files. Please contact the API administrators for further information.");
                }
            }

            
        }
    }
}
