using Newtonsoft.Json;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Layouts;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Core.Resources;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Xml;
using WebUtil = Sitecore.Web.WebUtil;

namespace Sitecore.Forms.Core.Commands
{
    [Serializable]
    public class EditForm : WebEditCommand
    {
        protected void CheckChanges(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (args.Result == "yes")
                {
                    args.Parameters["save"] = "1";
                    args.IsPostBack = false;
                    Context.ClientPage.Start(this, "Run", args);
                }
            }
            else
            {
                bool flag = false;
                if (args.Parameters["checksave"] != "0")
                {
                    FormItem form = FormItem.GetForm(args.Parameters["formId"]);
                    if (this.GetModifiedFields(form).Count<PageEditorField>() > 0)
                    {
                        flag = true;
                        SheerResponse.Confirm(ResourceManager.Localize("ONE_OR_MORE_ITEMS_HAVE_BEEN_CHANGED"));
                        args.WaitForPostBack();
                    }
                }
                if (!flag)
                {
                    args.IsPostBack = false;
                    Context.ClientPage.Start(this, "Run", args);
                }
            }
        }

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
                args.CustomData.Add("form", context.Items[0].Database.GetItem(str));
                Context.ClientPage.Start(this, "CheckChanges", args);
            }
        }

        private IEnumerable<PageEditorField> GetModifiedFields(FormItem form)
        {
            List<PageEditorField> list = new List<PageEditorField>();
            if (form != null)
            {
                foreach (PageEditorField field in WebEditCommand.GetFields(Context.ClientPage.Request.Form))
                {
                    Item item = StaticSettings.ContextDatabase.GetItem(field.ItemID);
                    if ((form.GetField(field.ItemID) != null) || (item.ID == form.ID))
                    {
                        string strB = item[field.FieldID];
                        string strA = field.Value;
                        if ((string.Compare(strA, strB, true) != 0) && (string.Compare(strA.TrimWhiteSpaces(), strB.TrimWhiteSpaces()) != 0))
                        {
                            list.Add(field);
                        }
                    }
                }
            }
            return list;
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                FormItem form = FormItem.GetForm(args.Parameters["formId"]);
                if ((form != null) && !args.HasResult)
                {
                    if (args.Parameters["save"] == "1")
                    {
                        this.SaveFields(form);
                    }
                    string str = args.Parameters["referenceId"];
                    UrlString str2 = new UrlString(UIUtil.GetUri("control:Forms.FormDesigner"));
                    str2["formid"] = form.ID.ToString();
                    str2["mode"] = StaticSettings.DesignMode;
                    str2["db"] = form.Database.Name;
                    str2["vs"] = form.Version.ToString();
                    str2["referenceId"] = form.Version.ToString();
                    str2["la"] = args.Parameters["contentlanguage"] ?? form.Language.Name;
                    if (args.Parameters["referenceId"] != null)
                    {
                        str2["hdl"] = str;
                    }
                    ApplicationItem application = ApplicationItem.GetApplication(Path.FormDesignerApplication);
                    string width = null;
                    string height = null;
                    if (application != null)
                    {
                        width = MainUtil.GetInt(application.Width, 0x4e2).ToString();
                        height = MainUtil.GetInt(application.Height, 500).ToString();
                    }
                    SheerResponse.ShowModalDialog(str2.ToString(), width, height, string.Empty, true);
                    SheerResponse.DisableOutput();
                    args.WaitForPostBack();
                }
            }
            else if (!string.IsNullOrEmpty(args.Parameters["scLayout"]))
            {
                ID id;
                SheerResponse.SetAttribute("scLayoutDefinition", "value", args.Parameters["scLayout"]);
                string str5 = args.Parameters["referenceId"];
                if (!string.IsNullOrEmpty(str5))
                {
                    str5 = "r_" + ID.Parse(str5).ToShortID();
                }
                string str6 = args.Parameters["formId"];
                if (ID.TryParse(str6, out id))
                {
                    str6 = id.ToShortID().ToString();
                }
                SheerResponse.Eval("window.parent.Sitecore.PageModes.ChromeManager.fieldValuesContainer.children().each(function(e){ if( window.parent.$sc('#form_" + str6.ToUpper() + "').find('#' + this.id + '_edit').size() > 0 ) { window.parent.$sc(this).remove() }});");
                SheerResponse.Eval("window.parent.Sitecore.PageModes.ChromeManager.handleMessage('chrome:rendering:propertiescompleted', {controlId : '" + str5 + "'});");
            }
        }

        private void SaveFields(FormItem form)
        {
            foreach (PageEditorField field in this.GetModifiedFields(form))
            {
                Item item = StaticSettings.ContextDatabase.GetItem(field.ItemID);
                item.Editing.BeginEdit();
                item[field.FieldID] = field.Value;
                item.Editing.EndEdit();
            }
        }
    }
}