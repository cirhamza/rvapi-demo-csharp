using REALvisionApiLib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace REALvisionApiLib
{
    // ************************************************************************************* //
    //  This is the Object we pass to every API call using the Initialize Request function.
    //  Each API Function has it's own ApiRequest instance that has different values.
    //  There is a different constructor for different kinds of requests.
    // ************************************************************************************* //

    public class ApiRequest
    {

    }

    public class UploadApiRequest : ApiRequest
    {
        public string FileName { get; set;}
        public byte[] File { get; set; }
        public UploadApiRequest(string fileName, byte[] file)
        {
            FileName = fileName;
            File = file;
        }
    }
    public class NoConfigApiRequest : ApiRequest
    {
        public string FileName { get; set; }
        public string FileId { get; set; }
        public String SupportType { get; set; }
        public String PrinterModel { get; set; }
        public String ConfigPresetName { get; set; }
        public PositionXyz Position { get; set; }
        public RotationXyz Rotation { get; set; }
        public ScaleXyz Scale { get; set; }

        public NoConfigApiRequest(
            string fileName,
            string fileId, 
            string supportType, 
            string printerModel, 
            string configPresetName, 
            PositionXyz position,
            RotationXyz rotation,
            ScaleXyz scale
            )
        {
            FileName = fileName;
            FileId = fileId;
            SupportType = supportType;
            PrinterModel = printerModel;
            ConfigPresetName = configPresetName;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
    }
    public class ConfigApiRequest : ApiRequest
    {
        public WsFile File { get; set; }
        public WsFile ConfigFile { get; set; }

        public ConfigApiRequest(WsFile file, WsFile configFile)
        {
            File = file;
            ConfigFile = configFile;
        }
    }
    public class TaskApiRequest : ApiRequest
    {
        public String TaskId { get; set; }

        public TaskApiRequest(string taskId)
        {
            TaskId = taskId;
        }
    }
}
