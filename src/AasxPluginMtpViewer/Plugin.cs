﻿using AdminShellNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The Newtonsoft.JSON serialization is licensed under the MIT License (MIT).
The Microsoft Microsoft Automatic Graph Layout, MSAGL, is licensed under the MIT license (MIT).
*/

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    public class AasxPlugin : IAasxPluginInterface // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    {
        private LogInstance Log = new LogInstance();
        private PluginEventStack eventStack = new PluginEventStack();
        private AasxPluginMtpViewer.MtpViewerOptions options = new AasxPluginMtpViewer.MtpViewerOptions();

        // private AasxPluginBomStructure.GenericBomControl bomControl = new AasxPluginBomStructure.GenericBomControl();

        private AasxPluginMtpViewer.WpfMtpControlWrapper viewerControl = new AasxPluginMtpViewer.WpfMtpControlWrapper();

        public string GetPluginName()
        {
            Log.Info("GetPluginName() = {0}", "MtpViewer");
            return "AasxPluginMtpViewer";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AasxPluginMtpViewer.MtpViewerOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt = AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginMtpViewer.MtpViewerOptions>(this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this.options = newOpt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public object CheckForLogMessage()
        {
            return Log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            Log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            // for speed reasons, have the most often used at top!
            res.Add(new AasxPluginActionDescriptionBase("call-check-visual-extension", "When called with Referable, returns possibly visual extension for it."));
            // rest follows
            res.Add(new AasxPluginActionDescriptionBase("set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(new AasxPluginActionDescriptionBase("get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(new AasxPluginActionDescriptionBase("get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));            
            res.Add(new AasxPluginActionDescriptionBase("fill-panel-visual-extension", "When called, fill given WPF panel with control for graph display."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // for speed reasons, have the most often used at top!
            if (action == "call-check-visual-extension")
            {
                // arguments
                if (args.Length < 1)
                    return null;

                // looking only for Submodels
                var sm = args[0] as AdminShell.Submodel;
                if (sm == null)
                    return null;

                // check for a record in options, that matches Submodel
                var found = false;
                if (this.options != null && this.options.Records != null)
                    foreach (var rec in this.options.Records)
                        if (rec.AllowSubmodelSemanticId != null)
                            foreach (var x in rec.AllowSubmodelSemanticId)
                                if (sm.semanticId != null && sm.semanticId.Matches(x))
                                {
                                    found = true;
                                    break;
                                }
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("MTP", "Module Type Package - View");

                // ok
                return cve;
            }

            // rest follows
            // logger.Log("ActivatePlugin() called with action = {0}", action);

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginMtpViewer.MtpViewerOptions>((args[0] as string));
                if (newOpt != null)
                    this.options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(this.options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The AutomationML.Engine is licensed under the MIT license (MIT) (see below).";

                lic.longLicense = "";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AasxPluginMtpViewer.Resources.LICENSE.txt"))
                {
                    if (stream != null)
                    {
                        TextReader tr = new StreamReader(stream);
                        lic.longLicense += tr.ReadToEnd();
                    }
                }

                return lic;
            }

            if (action == "get-events" && this.eventStack != null)
            {
                // try access
                return this.eventStack.PopEvent();
            }

            if (action == "get-check-visual-extension")
            {
                var cve = new AasxPluginResultBaseObject();
                cve.strType = "True";
                cve.obj = (Boolean)true;
                return cve;
            }

            if (action == "fill-panel-visual-extension" && this.viewerControl != null)
            {
                // arguments
                if (args.Length < 3)
                    return null;

                // call
                var resobj = AasxPluginMtpViewer.WpfMtpControlWrapper.FillWithWpfControls(args[0], args[1], this.options, this.eventStack, args[2]);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = resobj;
                return res;
            }

            // default
            return null;
        }

    }
}
