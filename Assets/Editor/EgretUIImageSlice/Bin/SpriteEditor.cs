using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Xml;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


/// <summary>
/// 这个函数是自动对图片资源自动处理，设置类型，格式等
/// </summary>
public class TextureEditor  {


    static Hashtable imageSeting = new Hashtable();

    static Object targetObj;

    [MenuItem("CellsGame/Tools/BatchSpriteSciliping")]
    static void BatchSpriteSciliping() {
        ReadEui2HashTable();
        UnityEngine.Object[] arr=Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);  
        for(int i=0;i < arr.Length;i++){
            //targetObj = Selection.activeObject;//这个函数可以得到选中的对象
            targetObj = arr[i];
            if (targetObj && targetObj is Texture)
            {
                string path = AssetDatabase.GetAssetPath(targetObj);
                TextureImporter texture = AssetImporter.GetAtPath(path) as TextureImporter;
                texture.textureType = TextureImporterType.Sprite;
                texture.spritePixelsPerUnit = 1;
                texture.spriteImportMode = SpriteImportMode.Single;
                texture.filterMode = FilterMode.Bilinear;
                texture.spritePixelsPerUnit = 100;
                texture.mipmapEnabled = false;
                var slict = GetSceliping(texture);
                var size = GetImageSize(texture);
                // var size = new Vector2(52,72);
                texture.spriteBorder = new Vector4(slict.x, size.y - slict.y - slict.w, size.x - slict.x - slict.z,slict.y);
                AssetDatabase.ImportAsset(path);
            }
        }

        Debug.Log("操作成功!");
    }

    static Vector2 GetImageSize(TextureImporter importer) {
        Vector2 v = new Vector2();
        System.Drawing.Image image = System.Drawing.Image.FromFile(importer.assetPath);
        v.x = image.Width;
        v.y = image.Height;
        return v;
    }

    static void LogWrite(string msg){
        if(IsDebug()) {
            Debug.Log(msg);
        }
        else {
            // not do anyting.
        }
    }

    static bool IsDebug() {
        chekCfg();
        return cfg["debug"].ToString().ToLower() == "true";
    }

    static Vector4 GetSceliping(TextureImporter textureImporter) {
        // string filename = System.IO.Path.GetFileName(textureImporter.assetPath);
        string filename = textureImporter.assetPath;
        foreach(string key in imageSeting.Keys)
        {   
            if(filename.Contains(key) == true){
                return (Vector4)(imageSeting[key]);
            }
        }
        return new Vector4(); 
    }

    static void ReadEui2HashTable (){
        //var files = System.IO.Directory.GetFiles(Application.dataPath + "/","*.exml");
         List<string>  files = GetFiles(GetEgretRootPath() + "/resource/ui");
        foreach(var file in files){  readXml2HashTable(file);  }
    }


    static JObject cfg;
    static string GetEgretRootPath() {
       chekCfg();
       return cfg["egregRoot"].ToString();
    }

    static void chekCfg(){
        if(cfg == null) {
             string json = System.IO.File.ReadAllText(Application.dataPath + "/Editor/TextureSliceByEgretEditor/TextureSliceCfg.json");
            cfg = JObject.Parse(json); 
        }
    }

    static List<string> GetFiles(string root){
        List<string> fileList = new List<string>();
        string[] folders = System.IO.Directory.GetDirectories(root);
        for (int i = folders.Length - 1; i >= 0 ; i--)
        {
             List<string> _flist = GetFiles(folders[i]);
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

    //<e:Image source="zizhai_json.zz_07" scaleX="1" scaleY="1" horizontalCenter="0" top="0" width="1000" height="71" scale9Grid="449,8,212,55"/>
    static void readXml2HashTable(string path) {
        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
        // try {
            doc.Load(path);
            LogWrite(path);
            var xmlNodes = doc.ChildNodes[1].ChildNodes;
            foreach(XmlNode xmlNode in xmlNodes){
                readImageNodeToHashTable(xmlNode,imageSeting,path);
            }
        // }
        // catch(System.Exception e){
        //     if(IsDebug())
        //         LogWrite(e.Message);
        //     else throw e;
        // }
    }

    static void readImageNodeToHashTable(XmlNode node,Hashtable hashtable,string path) {
        if(node.ChildNodes.Count > 0) {
            foreach(XmlNode xmlNode in node.ChildNodes) {
                readImageNodeToHashTable(xmlNode,hashtable,path);
            }
        }else {
            XmlAttributeCollection attributes = node.Attributes;
            if(attributes == null) {
                Debug.Log(path);
                return;
            }
            if(attributes["scale9Grid"] != null) {
                // string pngName = attributes["source"].Value.Replace("_json.","/");
                // Debug.Log(pngName + "=" + attributes["scale9Grid"].Value);
                string source = attributes["source"].Value;
                string[] scale9grid = attributes["scale9Grid"].Value.Split(',');
                string _path = ReadImageDirPathWithJson(source);
                if(_path == null || _path.Length == 0){
                    Debug.Log("解析eui出错:(检查default.res.json 是否与svn一致)"   + path );
                } else {

                    if(_path == "tongyong1/biaoti.png") {
                        Debug.Log(attributes["scale9Grid"].Value);
                    }
                    hashtable[_path] = new Vector4(float.Parse(scale9grid[0]),
                                                float.Parse(scale9grid[1]),
                                                float.Parse(scale9grid[2]),
                                                float.Parse(scale9grid[3]));
                }
            }
        }
    }

    static JToken jtoken ;
    static string ReadImageDirPathWithJson(string source){
        // if(jtoken == null) {
            string json = System.IO.File.ReadAllText(GetEgretRootPath() + "/resource/default.res.json");
            jtoken = JObject.Parse(json)["resources"];
        // }
        string[] split = source.Split('.');
        foreach(JToken item in jtoken) {
            string name = item["name"].ToString();
            if(name != split[0]) continue;
            //直接是图片的
            if(split.Length == 1 && item["type"].ToString() == "image" ) { 
                return item["url"].ToString();
            }
            //图集
            else if(split.Length == 2 && item["type"].ToString() == "sheet") {
                if(item["subkeys"].ToString().Contains(split[1]) == true){
                   var ret = System.IO.Path.GetFileName(item["url"].ToString().Replace(".json","")) + "/" + split[1] + ".png";//.Replace("_",".");
                   return ret;
                }
            }
            else {
                // throw new System.Exception("无法处理的资源类型");
            }

        }
        return null;
    }
}