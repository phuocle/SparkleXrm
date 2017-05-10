﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkleXrm.Tasks.Config
{
    public class ConfigFile
    {
        public List<WebresourceDeployConfig> webresources;
        public List<PluginDeployConfig> plugins;
        public List<EarlyBoundTypeConfig> earlyboundtypes;
        [JsonIgnore]
        public string filePath;
       
        public static List<ConfigFile> FindConfig(string folder, bool raiseErrorIfNotFound=true)
        {
            // search for the config file
            var configfilePath = DirectoryEx.Search(folder, "spkl.json",null);

            if (raiseErrorIfNotFound && (configfilePath==null || configfilePath.Count==0) )
            {
                throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.CONFIG_NOTFOUND, String.Format("Cannot find spkl.json in at '{0}' - make sure it is in the same folder or sub-folder as spkl.exe or provide a [path]", folder));
            }

            var results = new List<ConfigFile>();

            foreach (var configPath in configfilePath)
            {
                if (configPath != null)
                {
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(configPath));
                    config.filePath = Path.GetDirectoryName(configPath);
                    results.Add(config);
                }
            }
            
            if (results.Count==0)
            {
                results.Add(new ConfigFile
                {
                    filePath = folder
                });
            }

            return results;
        }

        public void Save()
        {
            var file = Path.Combine(filePath, "spkl.json");
            if (File.Exists(file))
            {
                File.Copy(file, file + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak");
            }
            File.WriteAllText(file, Newtonsoft.Json.JsonConvert.SerializeObject(this,Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
        }

        public EarlyBoundTypeConfig[] GetEarlyBoundConfig(string profile)
        {
            if (earlyboundtypes == null)
                return new EarlyBoundTypeConfig[] { new EarlyBoundTypeConfig(){
                    filename ="Entities.cs",
                    entities = "account,contact",
                    classNamespace = "Xrm",
                    generateOptionsetEnums = true,
                    generateStateEnums = true
                } };
                    
            EarlyBoundTypeConfig[] config = null;
            if (profile == "default")
            {
                profile = null;
            }
            if (profile != null)
            {
                config = earlyboundtypes.Where(c => c.profile != null && c.profile.Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = earlyboundtypes.Where(c => c.profile == null || c.profile.Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public WebresourceDeployConfig[] GetWebresourceConfig(string profile)
        {
            if (webresources == null)
                return new WebresourceDeployConfig[0];

            WebresourceDeployConfig[] config = null;
            if (profile == "default")
            {
                profile = null;
            }
            if (profile != null)
            {
                config = webresources.Where(c => c.profile != null && c.profile.Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = webresources.Where(c => c.profile==null || c.profile.Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public PluginDeployConfig[] GetPluginsConfig(string profile)
        {           
            PluginDeployConfig[] config = null;
            if (plugins == null)
                return new PluginDeployConfig[0];

            if (profile == "default")
            {
                profile = null;
            }

            if (profile != null)
            {
                config = plugins.Where(c => c.profile!=null && c.profile.Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = plugins.Where(c => c.profile==null || c.profile.Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public static List<string> GetAssemblies(ConfigFile config, PluginDeployConfig plugin)
        {
            List<string> GetAssemblies(List<string> configAssemblies)
            {
                if (configAssemblies == null) return new List<string>();
                var assemblies = new List<string>();
                foreach (string assembly in configAssemblies)
                {
                    var assemblyPath = Path.Combine(config.filePath, assembly);
                    var extension = Path.GetExtension(assemblyPath);
                    if (extension == "") assemblyPath = Path.Combine(assemblyPath, "*.dll");
                    var path = Path.GetDirectoryName(assemblyPath);
                    var file = Path.GetFileName(assemblyPath);
                    assemblies.AddRange(DirectoryEx.Search(path, file, null));
                }
                return assemblies;
            }
            var includeAssemblies = GetAssemblies(plugin.assemblies);
            var excludeAssemblies = GetAssemblies(plugin.excludeassemblies);
            return includeAssemblies.Except(excludeAssemblies).ToList();
        }
    }
}
