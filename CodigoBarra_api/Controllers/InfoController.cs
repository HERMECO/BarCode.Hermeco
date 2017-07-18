using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using CodigoBarra_api.Models;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
//using Spire.Barcode;
using System.IO;
using System.Web;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing.Imaging;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI;
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

        public byte[] Clave = Encoding.ASCII.GetBytes("Encrytion");// Clave de cifrado. NOTA: Puede ser cualquier combinación de carácteres.
        public byte[] IV = Encoding.ASCII.GetBytes("Devjoker7.37hAES");

        public string rute = Path.Combine(HttpContext.Current.Server.MapPath("/CodeBar")); // Ruta dinamica para la imagen

        // GET: api/Info/5
        public IEnumerable<InfoEncrypt> GetEncryption(string Correo, string Value, bool ValueEncript)
        {
            string email = Regex.Replace(Correo, @"\""", "");
            string code = Regex.Replace(Value, @"\""", "");
            
            if (!IsValidEmail(email))
            {
                email = "Por favor introduzca una dirección de correo válido";
                code = "";
            }
            else {
                if (ValueEncript)
                {
                    byte[] inputBytes = Encoding.ASCII.GetBytes(code);
                    byte[] encripted;
                    
                    RijndaelManaged cripto = new RijndaelManaged();
                    using (MemoryStream ms = new MemoryStream(inputBytes.Length))
                    {
                        using (CryptoStream objCryptoStream = new CryptoStream(ms, cripto.CreateEncryptor(Clave, IV), CryptoStreamMode.Write))
                        {
                            objCryptoStream.Write(inputBytes, 0, inputBytes.Length);
                            objCryptoStream.FlushFinalBlock();
                            objCryptoStream.Close();
                        }
                        encripted = ms.ToArray();
                    }
                    code = Regex.Replace(Convert.ToBase64String(encripted), @"[^0-9A-Za-z]", "", RegexOptions.None);
                }

                Barcode barcode = new Barcode();

                barcode.IncludeLabel = true;
                barcode.Alignment = AlignmentPositions.CENTER;
                barcode.Width = 500;
                barcode.Height = 100;
                Image img = barcode.Encode(TYPE.CODE39Extended, code);

                if (File.Exists($"{rute}\\{code}.png"))
                    File.Delete($"{rute}\\{code}.png");

                img.Save($"{rute}\\{code}.png", ImageFormat.Png);

                MailCode(email, code);
            }

            InfoEncrypt[] Data = new InfoEncrypt[] { new InfoEncrypt() { Email = email, CodeEncryp = code } };

            return Data;
        }

        public IEnumerable<InfoDesEncrypt> GetDesencrypted(string CodigoDesEncryp)
        {
            string DesEncription;
            string DesEncription1 = "";

            if (CodigoDesEncryp == "" || CodigoDesEncryp == null)
            {
                DesEncription1 = "Por favor incluya un valor valido para la des-encripcion";
            }
            else {
                DesEncription = Regex.Replace(CodigoDesEncryp, @"\""", "");


                string dummyData = DesEncription.Trim().Replace(" ", "+");
                if (dummyData.Length % 4 > 0)
                    dummyData = dummyData.PadRight(dummyData.Length + 4 - dummyData.Length % 4, '=');
                //byte[] byteArray = Convert.FromBase64String(dummyData);

                byte[] inputBytes = Convert.FromBase64String(dummyData);

                byte[] resultBytes = new byte[inputBytes.Length];
                string textoLimpio = String.Empty;
                RijndaelManaged cripto = new RijndaelManaged();
                using (MemoryStream ms = new MemoryStream(inputBytes))
                {
                    using (CryptoStream objCryptoStream = new CryptoStream(ms, cripto.CreateDecryptor(Clave, IV), CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(objCryptoStream, true))
                        {
                            textoLimpio = sr.ReadToEnd();
                        }
                    }
                }

                DesEncription1 = Regex.Replace(textoLimpio, @"\""", "");
            }

            InfoDesEncrypt[] DataDesEnceryp = new InfoDesEncrypt[] { new InfoDesEncrypt() { CodeDesEncryp = DesEncription1 } };

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
            body.Replace("<h2>title</h2>", $"<h4>Código generado: </h4> <label> {code} </label>");
            body.Replace("&#-image", @" <img src=""cid:code"" alt='Code'>");
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
                SmtpServer.Port = 587; //Puerto que utiliza Gmail para sus servicios
                //Especificamos las credenciales con las que enviaremos el mail
                SmtpServer.Credentials = new NetworkCredential(remite, passRemite);
                SmtpServer.EnableSsl = true;
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
