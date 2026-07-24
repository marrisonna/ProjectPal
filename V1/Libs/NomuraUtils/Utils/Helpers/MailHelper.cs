using System;
using System.Collections;
using System.Net.Mail;
using System.Net.Mime;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Utilities.Configuration;
using Utilities.Logging;

namespace Utilities.Helpers
{
    public class MailHelper
    {
        /// <summary>
        /// This will send an email to the support team
        /// please use this with care as this can potentially send out lots of emails.
        /// </summary>
        public static bool SendSupportEmail(string subject, string emailText)
        {
            return SendSupportEmail(subject, emailText, null, null);
        }
        
        /// <summary>
        /// This will send an email to the support team
        /// please use this with care as this can potentially send out lots of emails.
        /// </summary>
        public static bool SendSupportEmail(string subject, string emailText, Exception ex)
        {
            return SendSupportEmail(subject, emailText, ex, null);
        }


        /// <summary>
        /// The email address used for sending  mail.
        /// </summary>
        public static string EmailAddressToUse 
        { get 
            {
                string toEmailId = ABSConfig.GetValue("SupportEmailId");
                if (string.IsNullOrEmpty(toEmailId))
                {
                    toEmailId = @"ABSIT@uk.nomura.com";
                }
                return toEmailId;
            } 
        }

        /// <summary>
        /// This will send an email to the support team
        /// please use this with care as this can potentially send out lots of emails.
        /// </summary>
        public static bool SendSupportEmail(string subject, string emailText, Exception ex, Stream otherAttachment, string attachmentName)
        {
            Logger.TraceMethodEntry();
            bool mailSent = false;

            string sendEmails = ABSConfig.GetValue("EnableSupportEmail");
            bool shallSendEmails;
            if (Boolean.TryParse(sendEmails, out shallSendEmails) && true == shallSendEmails)
            {
                Logger.Debug("Preparing and sending email");
                string toEmailId = EmailAddressToUse;

                // this validates the email address format
                MailAddress toAddr = new MailAddress(toEmailId);
                StringBuilder subjectStr = new StringBuilder();

                MailMessage message = new MailMessage(toAddr, toAddr);
                message.Subject = subject;

                // body of the message
                message.BodyEncoding = Encoding.ASCII;
                StringBuilder messageBody = new StringBuilder(100);
                messageBody.AppendLine();
                messageBody.Append(emailText);
                messageBody.AppendLine();
                messageBody.AppendLine("----------------------------------------");
                messageBody.AppendFormat("Windows user name:  {0}", EnvironmentHelper.CompleteWindowsUserName);
                messageBody.AppendLine();
                messageBody.AppendFormat("Application Name :  {0}", CommonHelper.ApplicationName);
                messageBody.AppendLine();
                messageBody.AppendFormat("Log File Path    :  {0}", Logger.CurrentLogFilePath);
                messageBody.AppendLine();
                messageBody.AppendFormat("Sent from host   :  {0} ", System.Environment.MachineName);
                messageBody.AppendLine();

                bool hasAttachments = false;
                Stream attachmentStream = null;

                if (ex != null)
                {
                    string buildLogString = BuildLogString("", ex);

                    attachmentStream = new MemoryStream(UTF8Encoding.Default.GetBytes(buildLogString));

                    Attachment attachment = new Attachment(attachmentStream, System.Environment.MachineName + System.Environment.UserName + new Random().Next() + ".log"); ;
                    message.Attachments.Add(attachment);
                    hasAttachments = true;



                }

                if (otherAttachment != null && otherAttachment.CanRead)
                {
                    string name = attachmentName;

                    if (name.IsNullOrEmpty())
                    {
                        name = "Attachment.txt";
                    }
                    Attachment originalAttachment = new Attachment(otherAttachment, name);
                    message.Attachments.Add(originalAttachment);
                    hasAttachments = true;
                }

                if (hasAttachments)
                {
                    messageBody.AppendLine("Please see attachment for further details.");
                    messageBody.AppendLine();
                }
                messageBody.AppendLine("----------------------------------------");

                message.Body = messageBody.ToString();

                try
                {
                    // smtp server
                    string domain = EnvironmentHelper.UserDomain;

                    string defaultSMTP = "LONEV3101.EUROPE.NOM";

                    string configKey = domain + "_smtpHost";
                    Logger.Debug("Looking in config '{0}' for SMTP host", configKey);
                    string configSMTP = ABSConfig.GetValue(configKey);

                    string smtp = string.IsNullOrEmpty(configSMTP) ? defaultSMTP : configSMTP;

                    Logger.Debug("SMTP host is {0}", smtp);
                    SmtpClient mailClient = new SmtpClient(smtp);

                    mailClient.UseDefaultCredentials = true;
                    mailClient.Send(message);
                    mailSent = true;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, "Exception creating mail message");
                }
                finally
                {
                    if (attachmentStream != null)
                        attachmentStream.Close();
                }
                Logger.Debug("End of sending email");

            }
            else
            {
                Logger.Debug("Sending of emails has not been enabled. Config variable 'EnableSupportEmail' is not true.");
            }
            Logger.TraceMethodExit();
            return mailSent;
        }

        /// <summary>
        /// This will send an email to the support team
        /// please use this with care as this can potentially send out lots of emails.
        /// </summary>
        public static bool SendSupportEmail(string subject, string emailText, Exception ex, Stream otherAttachment)
        {
            return SendSupportEmail(subject, emailText, ex, otherAttachment, string.Empty);
        }

        // <summary>
        // log helper. Builds the exception log sting
        // </summary>
        private static string BuildLogString(string message, Exception ex)
        {
            StringBuilder strBuilder = null;

            if (message != null)
            {
                strBuilder = new StringBuilder();
                strBuilder.AppendLine();
                strBuilder.AppendLine("------------------------------------");
                strBuilder.AppendLine(string.Format("Message {0}:", message));
            }
            strBuilder.AppendLine(GetExceptionDetails(ex));
            strBuilder.AppendLine("------------------------------------");
            return strBuilder.ToString();
        }

        // <summary>
        // log helper. Builds the exception log details
        // </summary>
        private static string GetExceptionDetails(Exception ex)
        {
            StringBuilder strBuilder = null;
            if (ex != null)
            {
                strBuilder = new StringBuilder("Exception Occurred:");
                strBuilder.AppendLine();
                if (ex.StackTrace != null)
                {
                    strBuilder.AppendLine(string.Format("Exception Stack : {0}", ex.StackTrace));
                }

                if (ex.Message != null)
                {
                    strBuilder.AppendLine(string.Format("Exception Message: {0}", ex.Message));
                }

                if (ex.Data != null && ex.Data.Count > 0)
                {
                    strBuilder.AppendLine("Exception Data:");
                    strBuilder.AppendLine();
                    IDictionary dict = ex.Data;
                    if (dict != null && dict.Count > 0)
                    {
                        foreach (var key in dict.Keys)
                        {
                            strBuilder.AppendLine(string.Format("{0} = {1}", key, dict[key]));
                        }
                    }
                }

                if (ex.InnerException != null)
                {
                    strBuilder.AppendLine("Inner Exception:");
                    strBuilder.AppendLine(GetExceptionDetails(ex.InnerException));
                }
                return strBuilder.ToString();
            }
            return "";
        }
    }
}
