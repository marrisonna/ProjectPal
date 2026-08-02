using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{

    public class DocumentSearch
    {

        public enum DocumentType { OutLookMsg, Text, Word, Unknown };




        /// <summary>
        /// Pass in an array of strings to look for
        /// Returns an array of strings found
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public static string[] Search(string[] searchStrings, byte[] fileData, DocumentType type, bool searchAttachments)
        {
            if (fileData == null)
                return new string[0];

            List<SearchItem> searches = new List<SearchItem>();
            foreach (string text in searchStrings)
            {
                searches.Add(new SearchItem(text.ToLower()));
            }


            Search(searches, fileData, type, searchAttachments);

            List<string> result = new List<string>();
            foreach (SearchItem searchedForItem in searches)
            {
                if (searchedForItem.Found)
                    result.Add(searchedForItem.Text);
            }

            return result.ToArray();

        }


        private static void Search(List<SearchItem> searches, byte[] fileData, DocumentType type, bool searchAttachments)
        {

            List<SearchItem> outStandingSearches = new List<SearchItem>();
            foreach (SearchItem searchedForItem in searches)
            {
                if (!searchedForItem.Found)
                    outStandingSearches.Add(searchedForItem);
            }


            if (type == DocumentType.OutLookMsg)
                SearchOutLookMsg(outStandingSearches, fileData, searchAttachments);

            if (type == DocumentType.Word) // should never be true
                return; // SearchWordDoc(outStandingSearches, fileData); 

            if (type == DocumentType.Text)
                SearchText(outStandingSearches, fileData);


            if (type == DocumentType.Unknown)
                SearchText(outStandingSearches, fileData);

        }

        //private static void SearchWordDoc(List<SearchItem> searches, byte[] fileData)

        static private Microsoft.Office.Interop.Word.Application s_word = null;
        public static string[] SearchWordDoc(string[] searchStrings, string filePath)
        {
            // Taken from 
            // http://mantascode.com/c-how-to-parse-the-text-content-from-microsoft-word-document/
            //
            if (s_word == null)
                s_word = new Microsoft.Office.Interop.Word.Application();

            object miss = System.Reflection.Missing.Value;

            object readOnly = true;
            object filePathObject = filePath;
            Microsoft.Office.Interop.Word.Document wordDocument = s_word.Documents.Open(ref filePathObject, ref miss, ref readOnly, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss);
            StringBuilder totaltext = new StringBuilder();
            for (int i = 0; i < wordDocument.Paragraphs.Count; i++)
            {
                totaltext.Append(" \r\n " + wordDocument.Paragraphs[i + 1].Range.Text.ToString());
            }
            wordDocument.Close();
            //s_word.Quit();

            string totalTextStr = totaltext.ToString().ToLower();

            List<string> result = new List<string>();
            foreach (string searchedForItem in searchStrings)
            {
                if (totalTextStr.Contains(searchedForItem.ToLower()))
                    result.Add(searchedForItem);
            }

            return result.ToArray();
        }

        private static void SearchText(List<SearchItem> searches, byte[] fileData)
        {
            string text = System.Text.ASCIIEncoding.Default.GetString(fileData).ToLower();

            foreach (SearchItem searchedForItem in searches)
            {
                if (text.Contains(searchedForItem.Text))
                    searchedForItem.Found = true;
            }

        }



        private static void SearchOutLookMsg(List<SearchItem> searches, byte[] fileData, bool searchAttachments)
        {
            //string fileName = @"c:\tmp\outlook.msg";
            //string fileName2 = @"c:\tmp\outlook.txt";

            //System.IO.FileStream outFile = new System.IO.FileStream(fileName, System.IO.FileMode.Create);
            //outFile.Write(fileData, 0, fileData.Length);
            //outFile.Close();
            //iwantedue.OutlookStorage.Message outlookMsg = new iwantedue.OutlookStorage.Message(fileName);

            iwantedue.OutlookStorage.Message outlookMsg = new iwantedue.OutlookStorage.Message(new System.IO.MemoryStream(fileData, false));

            string body = "";
            try
            {
                body = outlookMsg.BodyText;
            }
            catch (Exception)
            {
                body = "";
                return;
            }
            if (body == null)
                body = "";


            string subject = "";
            try
            {
                subject = outlookMsg.Subject;
            }
            catch (Exception)
            {
                subject = "";
            }

            if (subject == null)
                subject = "";


            string text = subject + " " + body;

            text = text.ToLower();
            bool allFound = true;
            foreach (SearchItem searchedForItem in searches)
            {
                if (text.Contains(searchedForItem.Text))
                    searchedForItem.Found = true;
                else
                    allFound = false;
            }

            if (searchAttachments && !allFound)
            {
                List<iwantedue.OutlookStorage.Attachment> attachments = outlookMsg.Attachments;
                foreach (iwantedue.OutlookStorage.Attachment attachment in attachments)
                {
                    DocumentType type = DocumentType.Unknown;
                    string fileNameOrig = attachment.Filename;
                    StringBuilder fileName = new StringBuilder(fileNameOrig.Length);
                    foreach (char c in fileNameOrig)
                    {
                        if (c >= 32)
                            fileName.Append(c);
                    }

                    string fileExentions = Path.GetExtension(fileName.ToString()).ToLower().TrimStart(new char[] { '.' });
                    if (fileExentions == "msg")
                        type = DocumentType.OutLookMsg;

                    if (fileExentions.Length > 2 && fileExentions.Substring(0, 3) == "doc")
                        type = DocumentType.Word;

                    if (fileExentions == "txt")
                        type = DocumentType.Text;

                    Search(searches, attachment.Data, type, searchAttachments);

                }
            }


        }
    }

    internal class SearchItem
    {
        string m_searchText;

        public bool Found { get; set; }
        public string Text { get { return m_searchText; } }

        internal SearchItem(string searchText)
        {
            m_searchText = searchText;
            Found = false;
        }
    }

}

