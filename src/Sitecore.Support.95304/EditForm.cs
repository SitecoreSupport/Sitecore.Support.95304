using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Forms.Core.Data;
using Sitecore.Layouts;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;
using System.Web;
using System.Xml;
using WebUtil = Sitecore.Web.WebUtil;

namespace Sitecore.Support.Forms.Core.Commands
{
    [Serializable]
    public class EditForm : Sitecore.Forms.Core.Commands.EditForm
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            NameValueCollection parameters = new NameValueCollection();
            string str = context.Parameters["id"];
            bool flag = false;
            if (context.Items.Length > 0)
            {
                flag = FormItem.IsForm(context.Items[0]);
            }
            string formValue = WebUtil.GetFormValue("scLayout");
            parameters["sclayout"] = formValue;
            if (!flag && !string.IsNullOrEmpty(formValue))
            {
                ShortID tid;
                string xml = JsonConvert.DeserializeXmlNode(formValue).DocumentElement.OuterXml;
                string str4 = WebUtil.GetFormValue("scDeviceID");
                if (ShortID.TryParse(str4, out tid))
                {
                    str4 = tid.ToID().ToString();
                }
                RenderingDefinition renderingByUniqueId = LayoutDefinition.Parse(xml).GetDevice(str4).GetRenderingByUniqueId(context.Parameters["referenceId"]);
                if (renderingByUniqueId != null)
                {
                    WebUtil.SetSessionValue(StaticSettings.Mode, StaticSettings.DesignMode);
                    if (!string.IsNullOrEmpty(renderingByUniqueId.Parameters))
                    {
                        str = HttpUtility.UrlDecode(StringUtil.ParseNameValueCollection(renderingByUniqueId.Parameters, '&', '=')["FormID"]);
                    }
                }
            }
            XmlDocument document2 = JsonConvert.DeserializeXmlNode(formValue);
            string key = "PageDesigner";
            string outerXml = document2.DocumentElement.OuterXml;
            WebUtil.SetSessionValue(key, outerXml);
            if (!string.IsNullOrEmpty(str))
            {
                parameters["referenceid"] = context.Parameters["referenceId"];
                parameters["formId"] = str;
                parameters["checksave"] = context.Parameters["checksave"] ?? "1";
                if (context.Items.Length > 0)
                {
                    parameters["contentlanguage"] = context.Items[0].Language.ToString();
                }
                ClientPipelineArgs args = new ClientPipelineArgs(parameters);
                //args.CustomData.Add("form", context.Items[0].Database.GetItem(str));
                Context.ClientPage.Start(this, "CheckChanges", args);
            }
        }
    }
}