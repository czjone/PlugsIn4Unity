using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Xml;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class ExmlError {
    public string path;
    
    public string errorSource;
}

/// <summary>
/// 这个函数是自动对图片资源自动处理，设置类型，格式等
/// </summary>
public class CheckEuiWithRes  {

    static Hashtable allExistImages = new Hashtable();

    [MenuItem("CellsGame/Tools/CheckRes")]
    static void BatchSpriteSciliping() {
        InitAllExist();
        CheckAllFiles();
        CheckAllExml();

        UnityEngine.Debug.Log("检查完成,日志已生成");
    }

    static void CheckAllExml (){
         List<string>  files = GetAllFiles(GetEgretRootPath() + "/resource/ui");
        foreach(var file in files){  CheckExml(file);  }
    }

    static void InitAllExist() {
        string root = GetEgretRootPath();
        JToken defaultResJson ;
        string json = System.IO.File.ReadAllText(GetEgretRootPath() + "/resource/default.res.json");
        defaultResJson = JObject.Parse(json)["resources"];
        foreach(JToken item in defaultResJson) {
            string name = item["name"].ToString();
            string type = item["type"].ToString();
            if(type == "image") {
                if(type == "sheet") {
                    string url = item["url"].ToString();
                    url = url.Substring(0,url.Length - 4);
                    string sbkeys = item["subkeys"].ToString();
                    string[] images = sbkeys.Split(',');
                    foreach(var imgs in images) {
                        //allExistImages[imgs + "/" + imgs + "png"] = true;
                        allExistImages[name + "." + imgs + ".png"] = imgs + "/" + imgs + "png";
                    }
                }
                else if(type == "image") {
                    // allExistImages[item["url"].ToString()] = true;
                    allExistImages[name] = item["url"].ToString();
                }
            }else {
                continue;
            }
        }
    }

    static void CheckAllFiles() {
        string logfile = GetLogPath() + "/error.log";
        using (System.IO.FileStream fs = System.IO.File.Open(logfile,System.IO.FileMode.OpenOrCreate))
        {
            foreach(var item in allExistImages.Values) {
                string path = GetEgretRootPath() +"/resource/" + item;
                if(System.IO.File.Exists(path) == false) {
                    byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes("资源文件不存在:" + path + "\n\r");
                    fs.Write( bytes ,0,bytes.Length);        
                }
            }

            fs.Flush();
            fs.Close();
        }
    }


    static JObject cfg;
    static string GetEgretRootPath() {
       chekCfg();
       return cfg["egregRoot"].ToString();
    }

    static string GetLogPath() {
       chekCfg();
       return cfg["CheckResLog"].ToString();
    }

    static void chekCfg(){
        if(cfg == null) {
            string json = System.IO.File.ReadAllText(Application.dataPath + "/Editor/TextureSliceByEgretEditor/TextureSliceCfg.json");
            cfg = JObject.Parse(json); 
        }
    }

    static List<string> GetAllFiles(string root){
        List<string> fileList = new List<string>();
        string[] folders = System.IO.Directory.GetDirectories(root);
        for (int i = folders.Length - 1; i >= 0 ; i--)
        {
             List<string> _flist = GetAllFiles(folders[i]);
             foreach(var item in _flist){
                fileList.Add(item);
             }
        }

        var files = System.IO.Directory.GetFiles(root,"*.exml");
        for (int i = files.Length - 1; i >= 0 ; i--)
        {
            fileList.Add(files[i]);
        }
        return fileList;
    }

    static void CheckExml(string path) {
        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
        doc.Load(path);
        var xmlNodes = doc.ChildNodes[1].ChildNodes;
        List<ExmlError> error = new List<ExmlError>();
        foreach(XmlNode node in xmlNodes) CheckXmlNode(node,error, path);
        if(error.Count >0){
           string logfile = GetLogPath() + "/error.log";
            using (System.IO.FileStream fs = System.IO.File.Open(logfile,System.IO.FileMode.OpenOrCreate))
            {
                foreach(var item in error) {
                    byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes("tu pian  bu  chunzi  :" + item.errorSource  + "(" + item.path+ ")\r\n");
                    fs.Write( bytes ,0,bytes.Length);        
                }

                fs.Flush();
                fs.Close();
            }
        }
    }

    static void CheckXmlNode(XmlNode node,List<ExmlError> error,string path) {
        if(node.ChildNodes.Count > 0) {
            foreach(XmlNode _node in node.ChildNodes){   
                if(_node == null) Debug.Log(path);
                CheckXmlNode(_node,error,path);
            }
        }else {
            try {
                foreach(XmlAttribute att in node.Attributes) {     
                    if(att != null && att.Name == "source") {
                        if(allExistImages.ContainsKey(att.Value) == false) {
                            ExmlError e = new ExmlError();
                            e.path = path;
                            e.errorSource = att.Value;
                            error.Add(e);
                        }
                    }
                }
            }
            catch(System.Exception e){
                    Debug.Log("path:" + path +  "e:" + e);
            }
        }
    }
}