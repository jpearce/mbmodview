using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MBModViewer
{
    internal static class StaticDataHolder
    {
        internal static DataItemSettings[] ItemSettings;
        internal static DataReaderSettings[] ReaderSettings;
        internal static DataReader[] DataReaders;
        internal static PythonReader Header_Common;
        internal static PythonReader Header_Operations;
        internal static PythonReader Header_Triggers;

        internal static void LoadAll()
        {
            LoadDataItems();
            LoadDataReaders();
            CreateDataReaders();
            ReadHeaderCommon();
            ReadHeaderOperations();
            ReadHeaderTriggers();
        }

        internal static void ReadHeaderCommon()
        {
            Header_Common = new PythonReader(null, Config.GetSetting("filelocation_pydir") + "header_common.py");
            Header_Common.Read();
        }

        internal static void ReadHeaderOperations()
        {
            Header_Operations = new PythonReader(null, Config.GetSetting("filelocation_pydir") + "header_operations.py");
            Header_Operations.Read();
        }

        internal static void ReadHeaderTriggers()
        {
            Header_Triggers = new PythonReader(null, Config.GetSetting("filelocation_pydir") + "header_triggers.py");
            Header_Triggers.Read();
        }

        internal static String FindVarName(String typename, Int32 ID)
        {
            String tofind = typename.Replace("[", String.Empty).Replace("]", string.Empty).Trim().ToLower();
            for (int i = 0; i < DataReaders.Length; ++i)
            {
                if (tofind == DataReaders[i].Name)
                {
                    return "\"" + (typename == "[variable]" ? "$" : typename == "[quick_string]" ? "@" : String.Empty) +
                        DataReaders[i].Items[ID].Name + "\"";
                }
            }
            return null;
        }

        /// <summary>Since a true decompilation will show the string in a quickstring we show that</summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        internal static String FindQuickString(Int32 ID)
        {           
            
            for (int i = 0; i < DataReaders.Length; ++i)
            {
                if (DataReaders[i].Name == "quick_string")
                {
                    String retVal = DataReaders[i].Items[ID].Source.Replace("\r", String.Empty).Replace("\n", String.Empty);
                     if (retVal.IndexOf(' ') > -1) { return retVal.Substring(retVal.IndexOf(' ') + 1).Trim(); }
                     return retVal.Trim();
                }
            }
            return null;
        }

        static StaticDataHolder()
        {
            //if we load here then we won't get a real error message we'll just get 'type initializer for '
        }

        #region DataReader creation
        internal static void CreateDataReaders()
        {
            DataReaders = new DataReader[ReaderSettings.Length];
            for (int i = 0; i < ReaderSettings.Length; ++i)
            {
                DataReaders[i] = new DataReader(ReaderSettings[i]);                
                DataReaders[i].Read();
            }
            Console.Write("");
        }
        #endregion

        #region DataReader settings
        internal static void LoadDataReaders()
        {
            FileInfo fi_dataitems = new FileInfo(Config.GetSetting("filelocation_xmlsettings") + "DataReaders.xml");
            if (!fi_dataitems.Exists)
                throw new FileNotFoundException("DataReader.xml not found!");
            Exception xmlex = null;            
            List<DataReaderSettings> readers = new List<DataReaderSettings>();            
            using (XmlReader xr = new XmlTextReader(fi_dataitems.OpenText()))
            {
                try
                {
                    xr.ReadStartElement();
                    while (xr.Read())
                    {
                        if (xr.NodeType == XmlNodeType.Element && xr.Name == "DataReader")
                        {
                            DataReaderSettings drs = null;
                            String temp = xr.GetAttribute("name");
                            if (String.IsNullOrEmpty(temp))
                                throw new FormatException("DataReader name attribute not found.");
                            drs = new DataReaderSettings(temp);
                            String filename = xr.GetAttribute("filename");
                            if (String.IsNullOrEmpty(filename))
                                throw new FormatException("DataReader filename attribute not found.");
                            temp = xr.GetAttribute("dirsetting");
                            if (String.IsNullOrEmpty(temp)) { temp = "./"; }
                            else
                            {
                                temp = Config.GetSetting(temp);
                                if (String.IsNullOrEmpty(temp)) { temp = "./"; }                                
                            }
                            drs.FileLocation = new FileInfo(temp + filename);
                            temp = xr.GetAttribute("dataitem");
                            if (String.IsNullOrEmpty(temp))
                                throw new FormatException("DataReader dataitem attribute not found.");
                            drs.DataType = null;
                            for (int i = 0; i < ItemSettings.Length; ++i)
                            {
                                if (ItemSettings[i].Name.ToLowerInvariant() == temp.ToLowerInvariant())
                                {
                                    drs.DataType = ItemSettings[i];
                                }
                            }
                            if (ReferenceEquals(drs.DataType,null))
                                throw new FormatException("DataReader dataitem value " + temp + " not found in loaded DataItems.");
                            Int32 startline = 0;
                            temp = xr.GetAttribute("startline");
                            Int32.TryParse(xr.GetAttribute("startline"), out startline);
                            drs.StartLine = startline;
                            readers.Add(drs);
                        }
                    }
                }
                catch (Exception ex) { xmlex = ex; }
                finally
                {
                    xr.Close();
                }
                //rethrow exception if it exists after we've ensured the xml file is closed
                if (!ReferenceEquals(xmlex, null))
                    throw xmlex;


            }
            ReaderSettings = readers.ToArray();
        }
        #endregion

        #region DataItems settings
        internal static void LoadDataItems()
        {
            FileInfo fi_dataitems = new FileInfo(Config.GetSetting("filelocation_xmlsettings") + "dataitems.xml");
            if (!fi_dataitems.Exists)
                throw new FileNotFoundException("DataItems.xml not found!");
            Exception xmlex = null;
            DataItemSettings dis = null;
            List<DataItemSettings> dataitems = new List<DataItemSettings>();            
            using (XmlReader xr = new XmlTextReader(fi_dataitems.OpenText()))
            {
                try
                {
                    xr.ReadStartElement();
                    while (xr.Read())
                    {
                        if (xr.NodeType == XmlNodeType.Element)
                        {
                            if (xr.Name == "DataItem" && xr.NodeType != XmlNodeType.EndElement)
                            {
                                String name = xr.GetAttribute("name");
                                if (String.IsNullOrEmpty(name))
                                    throw new FormatException("DataItem name not found.");
                                dis = new DataItemSettings(name);
                                while (xr.Name != "Processing" && xr.Read()) ;
                                if (xr.Name == "Processing" && xr.NodeType != XmlNodeType.EndElement)
                                {
                                    while (xr.Read())
                                    {
                                        if (xr.NodeType == XmlNodeType.Element || xr.IsEmptyElement)
                                        {
                                            if (xr.Name == "line")
                                            {
                                                if (xr.NodeType == XmlNodeType.EndElement || xr.IsEmptyElement)
                                                {//add a lineend type for <line/> or </line>                                                    
                                                    dis.LineItemTypes.Add(LineItemTypes.LineEnd);
                                                    dis.LineItemIDs.Add(null);
                                                }
                                            }
                                            else if (xr.Name == "item")
                                            {
                                                if (xr.NodeType != XmlNodeType.EndElement)
                                                {
                                                    String type = xr.GetAttribute("type");
                                                    switch (type)
                                                    {
                                                        case "space":
                                                            dis.LineItemTypes.Add(LineItemTypes.Space);
                                                            dis.LineItemIDs.Add(null);
                                                            break;
                                                        case "string":
                                                            dis.LineItemTypes.Add(LineItemTypes.String);
                                                            type = xr.GetAttribute("id");
                                                            if(type == null)
                                                                throw new FormatException("ID missing in data item in DataItems->Line->Type");
                                                            dis.LineItemIDs.Add(type);
                                                            break;
                                                        case "number":
                                                            dis.LineItemTypes.Add(LineItemTypes.Int64);
                                                            type = xr.GetAttribute("id");
                                                            if (type == null)
                                                                throw new FormatException("ID missing in data item in DataItems->Line->Type");
                                                            dis.LineItemIDs.Add(type);
                                                            break;
                                                        default:
                                                            throw new FormatException("Unknown type encountered in DataItems->Line->Type");                                                            
                                                    }
                                                }
                                            }                                            
                                        }                                        
                                        else if (xr.Name == "DataItem" && xr.NodeType == XmlNodeType.EndElement)
                                        {
                                            dataitems.Add(dis);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        
                    }
                }
                catch (Exception ex) { xmlex = ex; }
                finally
                {
                    xr.Close();
                }
                //rethrow exception if it exists after we've ensured the xml file is closed
                if (!ReferenceEquals(xmlex, null))
                    throw xmlex;


            }
            ItemSettings = dataitems.ToArray();
        }
        #endregion

    }
}
