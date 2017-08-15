using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using CodigoBarra_api.Models;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing.Imaging;
using System.Configuration;
using System.Drawing;
using BarcodeLib;

namespace CodigoBarra_api.Controllers
{
    public class InfoController : ApiController
    {
        // GET: api/Info
        public string Get()
        {
            return "Por favor incluya 3 parametros validos para encriptar ('Email', 'Codigo', 'ValueEncript')";
        }
        
        public string rute = Path.Combine(HttpContext.Current.Server.MapPath("/CodeBar")); // Ruta dinamica para la imagen

        // Se encripta la cadena
        public static string ConvertStringToHex(String input, Encoding encoding)
        {
            Byte[] stringBytes = encoding.GetBytes(input);
            StringBuilder sbBytes = new StringBuilder(stringBytes.Length);
            foreach (byte b in stringBytes)
            {
                sbBytes.AppendFormat("{0:X2}", b);
            }
            return sbBytes.ToString();
        }

        // GET: api/Info/5
        public IEnumerable<InfoEncrypt> GetEncryption(string Correo, string Value, bool ValueEncript)
        {
            string email = String.Empty;
            string code = String.Empty;

            if (Correo == null || Correo == "\"\"" || Value == null || Value == "\"\"" )
            {
                if (Correo == null || Correo == "\"\"")
                {
                    email = "Porfavor introduzca un valor para el parametro (Correo)";
                    code = "";
                }
                else if (Value == null || Value == "\"\"")
                {
                    email = "";
                    code = "Porfavor introduzca un valor para el parametro (Value)";
                }
            }
            else {
                email = Regex.Replace(Correo, @"\""", "");
                code = Regex.Replace(Value, @"\""", "");

                if (!IsValidEmail(email))
                {
                    email = "Por favor introduzca una dirección de correo válida";
                    code = "";
                }
                else
                {
                    if (ValueEncript)
                    {
                        code = ConvertStringToHex(code, Encoding.Unicode);
                    }

                    Barcode barcode = new Barcode();

                    barcode.IncludeLabel = true;
                    barcode.Alignment = AlignmentPositions.CENTER;
                    barcode.Width = 600;
                    barcode.Height = 150;
                    Image img = barcode.Encode(TYPE.CODE39Extended, code);

                    img.Save($"{rute}\\{code}.png", ImageFormat.Png);
                    MailCode(email, code);

                    //if (!File.Exists($"{rute}\\{code}.png"))
                    //{
                    //    img.Save($"{rute}\\{code}.png", ImageFormat.Png);
                    //    MailCode(email, code);
                    //}
                    //else
                    //{
                    //    email = "";
                    //    code = "El valor ya existe, por favor vuelva a intentarlo con otro valor";
                    //}
                }
            }

            InfoEncrypt[] Data = new InfoEncrypt[] { new InfoEncrypt() { Email = email, CodeEncryp = code } };

            return Data;
        }

        // Se des-encripta la cadena 
        public static string ConvertHexToString(String hexInput, Encoding encoding)
        {
            int numberChars = hexInput.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexInput.Substring(i, 2), 16);
            }
            return encoding.GetString(bytes);
        }

        public IEnumerable<InfoDesEncrypt> GetDesencrypted(string CodigoDesEncryp)
        {
            string DesEncription = String.Empty;

            if (CodigoDesEncryp == "\"\"" || CodigoDesEncryp == null)
            {
                DesEncription = "Porfavor introduzca un valor para el parametro (CodigoDesEncryp)";
            }
            else {
                DesEncription = Regex.Replace(CodigoDesEncryp, @"\""", "");
                DesEncription = ConvertHexToString(DesEncription, Encoding.Unicode);
            }

            InfoDesEncrypt[] DataDesEnceryp = new InfoDesEncrypt[] { new InfoDesEncrypt() { CodeDesEncryp = DesEncription } };

            return DataDesEnceryp;
        }

        public static bool IsValidEmail(string email)
        {
            string expresion;
            expresion = "\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*";
            if (Regex.IsMatch(email, expresion))
            {
                if (Regex.Replace(email, expresion, string.Empty).Length == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public string htmlImage(string code) {

            string html = File.ReadAllText(HttpContext.Current.Server.MapPath("~/index.html"));

            StringBuilder body = new StringBuilder();
            body.AppendLine(html);
            body.Replace("<h2 class='title-barcode'>Api BarCode Hermeco</h2>", $"<h4>Código generado: </h4> <label> {code} </label>");
            body.Replace("<div class='image-barcode'>", @"<div>");
            body.Replace("&#-image", @"<img src=""cid:code"" alt='Code'>");
            return body.ToString();
        }

        public Boolean MailCode(string email, string code)
        {
            try
            {
                string remite = ConfigurationManager.AppSettings["email"];
                string passRemite = ConfigurationManager.AppSettings["password"];
                string smtp = ConfigurationManager.AppSettings["smt"];

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlImage(code), Encoding.UTF8, MediaTypeNames.Text.Html);

                LinkedResource img1 = new LinkedResource(HttpContext.Current.Server.MapPath($"/CodeBar/{code}.png"), MediaTypeNames.Image.Jpeg);
                img1.ContentId = "code";
                htmlView.LinkedResources.Add(img1);
                
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtp);
                mail.From = new MailAddress(remite, "Codigo de barra generado", Encoding.UTF8);
                mail.Subject = "Código de barra";
                mail.IsBodyHtml = true;
                mail.AlternateViews.Add(htmlView);
                mail.To.Add(email);

                //Configuracion del SMTP
                SmtpServer.Port = 25;
                SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.Host = smtp;
                
                SmtpServer.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

    }
}
