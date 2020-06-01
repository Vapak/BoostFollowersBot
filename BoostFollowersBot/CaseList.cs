using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


namespace BoostFollowersBot
{
    static class CaseList
    {
        public static void SaveCase(long chatId)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("CaseState.xml");
            XmlElement xRoot = doc.DocumentElement;
            XmlNode xUser = xRoot.SelectSingleNode("user[@id='current']").Clone();

            foreach (XmlElement xNode in xRoot)
                if (xNode.GetAttribute("id") == chatId.ToString())
                {
                    xNode.InnerXml = xUser.InnerXml;
                    doc.Save("CaseState.xml");
                    return;
                }
            ((XmlElement)xUser).SetAttribute("id", chatId.ToString());

            xRoot.AppendChild(xUser);
            doc.Save("CaseState.xml");
        }

        public static void IncludeNickname(string nickname, long chatId)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("CaseState.xml");
            XmlElement xRoot = doc.DocumentElement;

            List<string> nicknames = GetNicknames(chatId.ToString());

            Queue<string> queue = new Queue<string>(nicknames);
            queue.Enqueue(nickname);
            queue.Dequeue();

            nicknames = new List<string>(queue);
            SetNicknames(nicknames);
        }

        public static List<string> GetNicknames(string id = "current")
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("CaseState.xml");
            XmlElement xRoot = doc.DocumentElement;

            XmlNode xList = xRoot.SelectSingleNode($"user[@id='{id}']/list");
            string[] nicks = xList.InnerText.Split(';', StringSplitOptions.RemoveEmptyEntries);
       
            return nicks.ToList<string>();
        }

        public static void SetNicknames(List<string> nicknames)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("CaseState.xml");
            XmlElement xRoot = doc.DocumentElement;

            string xListText = "";

            foreach(string nick in nicknames)
            {
                if (nick != nicknames.Last())
                    xListText += $"{nick};";
                else
                    xListText += $"{nick}";
            }

            XmlNode xList = xRoot.SelectSingleNode($"user[@id='current']/list");
            try
            {
                xList.InnerText = xListText;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }            
            doc.Save("CaseState.xml");
        }

        public static string GetUsername(long chatId)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("CaseState.xml");
            XmlElement xRoot = doc.DocumentElement;

            XmlNode xList = xRoot.SelectSingleNode($"user[@id='{chatId}']");
            return ((XmlElement)xList).GetAttribute("username");            
        }

        public static void SetUsername(long chatId, string username)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("CaseState.xml");
            XmlElement xRoot = doc.DocumentElement;
            try
            {
                XmlNode xList = xRoot.SelectSingleNode($"user[@id='{chatId}']");
                ((XmlElement)xList).SetAttribute("username", username);
                doc.Save("CaseState.xml");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }            
        }
    }
}
