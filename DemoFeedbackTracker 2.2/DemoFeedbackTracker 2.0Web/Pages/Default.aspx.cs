using FeedbackTracker.Common;
using FeedbackTracker.Common.Models;
using FeedbackTracker.Common.SQL;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace DemoFeedbackTracker_2._0Web
{
    public partial class Default : System.Web.UI.Page
    {
        SharePointContext spContext = null;
        string accessToken = null;

        protected void Page_PreInit(object sender, EventArgs e)
        {
            ClientScript.GetPostBackEventReference(this, "");
            Uri redirectUrl;
            switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
            {
                case RedirectionStatus.Ok:
                    return;
                case RedirectionStatus.ShouldRedirect:
                    Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
                    break;
                case RedirectionStatus.CanNotRedirect:
                    // Response.Write("An error occurred while processing your request.");
                    // Response.End();
                    break;
            }

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // button controls 
            btnApplyCustomization.Click += BtnApplyCustomization_Click;
            btnDisableCustomization.Click += BtnDisableCustomization_Click;

            spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

            using (var cc = spContext.CreateAppOnlyClientContextForSPAppWeb())
            {
                RegisterAddingWebHookEvent(cc);
                FixLookupColumns(cc);
                RegisterAsyncTask(new PageAsyncTask(ReactToWebHookDeletion));
                RegisterAsyncTask(new PageAsyncTask(ExecuteWebHooksLogic));
            }
        }

        private async Task ReactToWebHookDeletion()
        {
            string target = Request["__EVENTTARGET"];
            if (target == "deletewebhook")
            {
                using (var cc = spContext.CreateAppOnlyClientContextForSPAppWeb())
                {
                    string[] parameters = Request["__EVENTARGUMENT"].Split(new string[] { "||" }, StringSplitOptions.None);
                    string id = parameters[0];
                    string listId = parameters[1];

                    // Hookup event to capture access token
                    cc.ExecutingWebRequest += Cc_ExecutingWebRequest;
                    // Just load the Url property to trigger the ExecutingWebRequest event handler to fire
                    cc.Load(cc.Web, w => w.Url);
                    cc.ExecuteQueryRetry();

                    WebHookManager webHookManager = new WebHookManager();
                    // delete the web hook
                    if (await webHookManager.DeleteListWebHookAsync(cc.Web.Url, listId, id, this.accessToken))
                    {
                        using (SharePointWebHooks dbContext = new SharePointWebHooks())
                        {
                            var webHookRow = await dbContext.ListWebHooks.FindAsync(new Guid(id));
                            if (webHookRow != null)
                            {
                                dbContext.ListWebHooks.Remove(webHookRow);
                                var saveResult = await dbContext.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
        }

        private void RegisterAddingWebHookEvent(ClientContext cc)
        {
            btnCreate.Click += async (s, args) =>
            {
                // Hookup event to capture access token
                cc.ExecutingWebRequest += Cc_ExecutingWebRequest;

                var lists = cc.Web.Lists;
                Guid listId = new Guid(ListDropDown.SelectedItem.Text.Split(new string[] { "||" }, StringSplitOptions.None)[1]);
                IEnumerable<List> sharePointLists = cc.LoadQuery<List>(lists.Where(lst => lst.Id == listId));
                cc.Load(cc.Web, w => w.Url);
                cc.ExecuteQueryRetry();

                WebHookManager webHookManager = new WebHookManager();
                var res = await webHookManager.AddListWebHookAsync(cc.Web.Url, listId.ToString(),
                    "https://pnpwebhooksdemo.azurewebsites.net/api/webhooks", this.accessToken);

                // persist the latest changetoken of the list when we create a new webhook. This allows use to only grab the changes as of web hook creation when the first notification comes in
                using (SharePointWebHooks dbContext = new SharePointWebHooks("pnpwebhooksdemoEntities"))
                {
                    dbContext.ListWebHooks.Add(new ListWebHook()
                    {
                        Id = new Guid(res.Id),
                        StartingUrl = cc.Web.Url,
                        ListId = listId,
                        LastChangeToken = sharePointLists.FirstOrDefault().CurrentChangeToken.StringValue,
                    });
                    var saveResult = await dbContext.SaveChangesAsync();

                }
            };
        }

        private async Task ExecuteWebHooksLogic()
        {
            using (var cc = spContext.CreateAppOnlyClientContextForSPAppWeb())
            {
                cc.ExecutingWebRequest += Cc_ExecutingWebRequest;

                var lists = cc.Web.Lists;
                cc.Load(cc.Web, w => w.Url);
                cc.Load(lists, l => l.Include(p => p.Title, p => p.Id, p => p.Hidden));
                cc.ExecuteQueryRetry();

                WebHookManager webHookManager = new WebHookManager();

                // Grab the current lists
                List<SharePointList> modelLists = new List<SharePointList>();
                List<SubscriptionModel> webHooks = new List<SubscriptionModel>();

                foreach (var list in lists)
                {
                    if (!list.Hidden)
                    {
                        modelLists.Add(new SharePointList() { Title = list.Title, Id = list.Id });
                        var existingWebHooks = await webHookManager.GetListWebHooksAsync(cc.Web.Url, list.Id.ToString(), this.accessToken);

                        if (existingWebHooks.Value.Count > 0)
                        {
                            foreach (var existingWebHook in existingWebHooks.Value)
                            {
                                webHooks.Add(existingWebHook);
                            }
                        }
                    }
                }

                SharePointSiteModel sharePointSiteModel = new SharePointSiteModel();
                sharePointSiteModel.Lists = modelLists;
                sharePointSiteModel.WebHooks = webHooks;
                sharePointSiteModel.SelectedSharePointList = modelLists[0].Id;


                phWebHookTable.Controls.Clear();
                if (sharePointSiteModel.WebHooks.Count() == 0)
                {
                    phWebHookTable.Controls.Add(new Literal() { Text = "No web hooks..." });
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<table><tr><th>Actions</th><th>ID</th><th>List name</th><th>Notification URL</th><th>Expiration time</th></tr>");
                    foreach (var webHook in sharePointSiteModel.WebHooks)
                    {
                        var list = sharePointSiteModel.Lists.Where(f => f.Id == new Guid(webHook.Resource)).FirstOrDefault();
                        string listName = "";
                        if (list != null)
                        {
                            listName = String.Format("{0} - {1}", list.Title, webHook.Resource);
                        }
                        sb.Append($"<tr>");
                        sb.Append($"<td><a href='javascript:__doPostBack(\"deletewebhook\",\"{webHook.Id}||{list.Id.ToString("D")}\");'>Delete</a></td>");
                        sb.Append($"<td>{webHook.Id}</td>");
                        sb.Append($"<td>{listName}</td>");
                        sb.Append($"<td>{webHook.NotificationUrl}</td>");
                        sb.Append($"<td>{webHook.ExpirationDateTime}</td>");
                        sb.Append($"</tr>");
                    }
                    sb.Append("</table>");
                    phWebHookTable.Controls.Add(new Literal() { Text = sb.ToString() });
                }

                ListDropDown.DataSource = from l in sharePointSiteModel.Lists
                                          select new System.Web.UI.WebControls.ListItem() { Value = l.Id.ToString("D"), Text = l.Title + "||" + l.Id.ToString("D") };
                ListDropDown.DataBind();

          
            }
        }

        private void Cc_ExecutingWebRequest(object sender, WebRequestEventArgs e)
        {
            // grab the OAuth access token as we need this token in our REST calls
            this.accessToken = e.WebRequestExecutor.RequestHeaders.Get("Authorization").Replace("Bearer ", "");
        }

        private void BtnApplyCustomization_Click(object sender, EventArgs e)
        {
            OutputMessage("Starting...", true);

            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                AddJsFile(clientContext, Page.Request.Url.GetLeftPart(UriPartial.Authority) + '/',
                    spContext.SPAppWebUrl.AbsoluteUri);
                //AddJsFile(clientContext, spContext.SPAppWebUrl.AbsoluteUri);
            }
        }

        private void BtnDisableCustomization_Click(object sender, EventArgs e)
        {
            OutputMessage("Starting...", true);

            using (var clientContext = spContext.CreateAppOnlyClientContextForSPHost())
            {
                RemoveJsFile(clientContext);
            }
        }

        private void RemoveJsFile(ClientContext ctx)
        {
            var hostWeb = ctx.Web;
            var existingActions = hostWeb.UserCustomActions;
            ctx.Load(existingActions);
            ctx.ExecuteQuery();

            for (var i = 0; i < existingActions.Count; i++)
            {
                var action = existingActions[i];
                if (action.Description == "feedbackCustomization" &&
                    action.Location == "ScriptLink")
                {
                    action.DeleteObject();
                }
            }
            ctx.ExecuteQuery();
            OutputMessage("Removed successfully!");

        }

        private void AddJsFile(ClientContext ctx, string scriptLocation, string appWebUrl)
        {
            var hostWeb = ctx.Web;
            var revision = Guid.NewGuid().ToString("D");
            var jsLink = string.Format("{0}Scripts/hostInjection.js?rev={1}", scriptLocation, revision);

            var scriptBlock = @"
            var demoFeedbackAppWebUrl = '" + appWebUrl + @"';
            var demoFeedbackProviderAppUrl = '" + scriptLocation + @"';  
           
//SPAppToken     
            var headID = document.getElementsByTagName('head')[0];
            var newScript = document.createElement('script');
            newScript.type = 'text/javascript';
            newScript.src = '" + jsLink + @"';
            headID.appendChild(newScript); 
            var newStyle = document.createElement('link');
            newStyle.type = 'text/css';
            newStyle.rel = 'stylesheet';
            newStyle.href = '" + scriptLocation + @"content/feedback.css';
            headID.appendChild(newStyle);";

            var existingActions = hostWeb.UserCustomActions;
            ctx.Load(existingActions);
            ctx.ExecuteQuery();

            for (var i = 0; i < existingActions.Count; i++)
            {
                var action = existingActions[i];
                if (action.Description == "feedbackCustomization" &&
                    action.Location == "ScriptLink")
                {
                    action.DeleteObject();
                }
            }

            ctx.ExecuteQuery();

            var newAction = existingActions.Add();
            newAction.Description = "feedbackCustomization";
            newAction.Location = "ScriptLink";

            newAction.ScriptBlock = scriptBlock;
            newAction.Update();
            ctx.Load(newAction);
            ctx.ExecuteQuery();
            OutputMessage("Added successfully!");
        }

        private void FixLookupColumns(ClientContext clientContext)
        {
            try
            {
                var appWeb = clientContext.Web;
                var areasList = appWeb.Lists.GetByTitle("Areas");
                var feedbackList = appWeb.Lists.GetByTitle("Feedback Tracker");
                var field = appWeb.AvailableFields.GetByInternalNameOrTitle("Area");
                var listField = feedbackList.Fields.GetByInternalNameOrTitle("Area");
                clientContext.Load(areasList);
                clientContext.Load(feedbackList);
                feedbackList.Update();
                var views = feedbackList.Views;

                clientContext.Load(field);
                clientContext.Load(listField);
                clientContext.ExecuteQuery();

                var fieldLookup = clientContext.CastTo<FieldLookup>(field);
                fieldLookup.LookupField = "Title";
                fieldLookup.UpdateAndPushChanges(true);

                var fieldLookupList = clientContext.CastTo<FieldLookup>(listField);
                fieldLookupList.LookupField = "Title";
                fieldLookupList.UpdateAndPushChanges(true);
                feedbackList.Update();

                clientContext.ExecuteQuery();
            }
            catch (Exception ex)
            {
                litError.Text = ex.Message;
            }
        }

        private void OutputMessage(string text, bool overwrite = false)
        {
            if (overwrite)
            {
                message.InnerText = string.Empty;
            }
            message.InnerText += text + Environment.NewLine;
        }
    }
}