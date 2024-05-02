using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
namespace parseOFXJson
{
    public class ParseOFX
    {

        public async Task<dynamic> ReadFromFile(string file)
        {
            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException("Arquivo não informado.");
            byte[] bytes = await File.ReadAllBytesAsync(file);
            return await ReadFile(bytes);
        }
        public async Task<dynamic> ReadFromBytes(byte[] file)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentNullException("Arquivo não informado.");
            return await ReadFile(file);
        }

        private async Task<string> ReadFileFromBytes(byte[] file)
        {
            using (MemoryStream m = new MemoryStream(file))
            {
                using (StreamReader sr = new StreamReader(m))
                {
                      return await sr.ReadToEndAsync();
                };
            }
        }
        private async Task<dynamic> ReadFile(byte[] file)
        {
            string data = await ReadFileFromBytes(file);
            string[] arrText = data.Split("<OFX>", 2);
            string[] arrHeader = arrText[0].Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Dictionary<string, string> dicHeader = new Dictionary<string, string>();
            Dictionary<string, string> dicDoc = new Dictionary<string, string>();
            foreach (var item in arrHeader)
            {
                var arr = item.Split(':');
                if (arr.Length == 2)
                    dicHeader.Add(arr[0], arr[1]);
            }

            //header
            string headerJson = JsonConvert.SerializeObject(dicHeader);

            string content = "<OFX>" + arrText[1];

            string contentProccess = PreXML(content);

            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();

            xml.LoadXml(contentProccess);

            string xmlToJson = JsonConvert.SerializeXmlNode(xml);

            dynamic obj = new { header = JsonConvert.DeserializeObject(headerJson), conteudo = JsonConvert.DeserializeObject(xmlToJson) };

            return obj;
        }

        private string PreXML(string pre)
        {
            string result = pre;

            // Remove espaços em branco entre tags
            result = Regex.Replace(result, @">\s+<", "><");

            // Remove espaços em branco antes de uma tag de fechamento
            result = Regex.Replace(result, @"\s+<", "<");

            // Remove espaços em branco após uma tag de fechamento
            result = Regex.Replace(result, @">\s+", ">");

            // Remove tags de fechamento se presentes
            result = Regex.Replace(result, @"<([A-Za-z0-9_]+)>([^<]+)<\/\1>", "<$1>$2");

            // Remove tags de ponto
            result = Regex.Replace(result, @"<([A-Z0-9_]*)+\.+([A-Z0-9_]*)>([^<]+)", "<$1$2>$3");

            // Adiciona tags de fechamento onde estão faltando
            result = Regex.Replace(result, @"<(\w+?)>([^<]+)", "<$1>$2</$1>");

            return result;
        }
    }
}