    using System;
    using System.Collections; 
    using System.Linq;
    using System.Reflection;   
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine; 

     
    namespace LD.Editor
    {
        public static partial class PackageManagerUtility
        {

            public static IEnumerator AddDependencyCoroutine(MissingDependency missing)
            {
                bool added = false;
                foreach (var (version, index) in missing.Versions.Select((item, index) => (item, index)))
                {
                    var addRequest = Client.Add(version);
                    while (!addRequest.IsCompleted) yield return null;

                    if (addRequest.Status == StatusCode.Failure)
                    {
                        if (addRequest.Error != null)
                        {
                            Debug.LogErrorFormat("{0} : {1}", addRequest.Error.errorCode, addRequest.Error.message);
                        }
                    }
                    else
                    {
                        added = true;
                        if (added) break;
                    }
                }
            }

            
         
            
            
            
            public static IEnumerator GetMissingDependencyCoroutine(Action<MissingDependency[]> onComplete)
            {
                var list = Client.List(true, true);
                while (!list.IsCompleted)
                    yield return null;

                var findMissing = list.Result
                    .SelectMany(info =>
                    {
                        return info.dependencies.Where(dep => !list.Result.Any(item => item.name == dep.name));
                    })
                    .GroupBy(info => info.name, info => info.version)
                    .Where(group => group.Any())
                    .Select(group => new MissingDependency
                    {
                        Name = group.Key,
                        Versions = group.ToArray()
                    })
                    .ToArray();
            
                onComplete?.Invoke(findMissing);
            }

          
            
            
            

            /// <summary>
            /// Merging Scopes.
            /// </summary>
            /// <param name="originScopes">original socpes</param>
            /// <param name="newOrAddtionalScopes">new scopes</param>
            /// <returns></returns>
            public static string[] MergeScopes(string[] originScopes, string[] newOrAddtionalScopes)
            {
                return originScopes.Union(newOrAddtionalScopes).ToArray<string>();
            }
     
            // Coroutine version
            public static IEnumerator GetScopesCoroutine(string registryName, Action<string[]> callback)
            {
                var registriesRequest = Client.List(true, true);
                while (!registriesRequest.IsCompleted)
                    yield return null;

                var registries = registriesRequest.Result;
                var registry = registries.FirstOrDefault(x => x.name == registryName);
                var scopes = Getter<string[]>((registry.GetType(), "scopes", BindingFlags.Instance | BindingFlags.NonPublic), registry);
                if (scopes == null) throw new Exception("GetScopes Cannot Found Scopes");
                callback?.Invoke(scopes);
            }

         
            public static IEnumerator UpdateScopedRegistryCoroutine(string registryName, string url, string[] scopes, Action<StatusCode> callback)
            {
                string id = "";
                var idRequest = GetRegistryIdCoroutine(registryName, (data) =>
                {
                    id = data;
                });
                while (idRequest.MoveNext())
                {
                    yield return null;
                } 
                Assembly assembly = Assembly.Load("UnityEditor.CoreModule");
                Type type = assembly.GetType("UnityEditor.PackageManager.UpdateScopedRegistryOptions", true);
                object option = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { registryName, url, scopes }, null);

                var request = CallMethod<Request<RegistryInfo>>("UpdateScopedRegistry", null, BindingFlags.NonPublic | BindingFlags.Static, new object[] { id, option });
                while (!request.IsCompleted)
                {
                    yield return null;
                }

                callback(request.Status);
            }

     
            
            
            public static IEnumerator AddScopeAndRegistryCoroutine(string registryName, string url, string[] scopes, Action<StatusCode> onComplete)
            {
                var request = CallMethod<Request<RegistryInfo>>("AddScopedRegistry", null, BindingFlags.NonPublic | BindingFlags.Static, new object[] { registryName, url, scopes });
                if (request != null)
                {
                    while (!request.IsCompleted)
                        yield return null;

                    if (request.Status == StatusCode.Failure)
                        Debug.LogError("Package registration failed");
                    else if (request.Status == StatusCode.Success)
                    {
                        var result = Getter<RegistryInfo>((typeof(Request<RegistryInfo>), "Result", BindingFlags.Default), request);
                    }

                    onComplete?.Invoke(request.Status);
                }
            }

          
            
            public static IEnumerator GetRegistryIdCoroutine(string registryName, Action<string> onComplete)
            {
                var getRegistriesRequest = Client.List(true, true);
                while (!getRegistriesRequest.IsCompleted)
                    yield return null;

                var regs = getRegistriesRequest.Result;
                foreach (var result in regs)
                {
                    if (result.name != registryName) continue;
                    var properties = result.GetType()
                        .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var idProp = properties.First(x => x.Name == "id");
                    var regId = idProp.GetValue(result).ToString();

                    onComplete?.Invoke(regId);
                    yield break;
                }

                onComplete?.Invoke(null);
            }

     
            
            
            public static IEnumerator GetRegistriesCoroutine(Action<RegistryInfo[]> onComplete)
            {
                var obj = CallMethod("GetRegistries", null, BindingFlags.NonPublic | BindingFlags.Static, null);

                if (obj is Request request)
                {
                    while (!request.IsCompleted)
                        yield return null;

                    if (request.Status == StatusCode.Failure)
                        Debug.LogError("Failed to get registries");
                    else if (request.Status == StatusCode.Success)
                    {
                        var props = request.GetType().GetProperties();
                        RegistryInfo[] Result = null;
                        foreach (var prop in props)
                        {
                            if (prop.Name == "Result")
                            {
                                Result = (RegistryInfo[])prop.GetValue(request);
                                onComplete(Result);
                                yield break;
                            }
                        }

                    }
                }
                else
                {
                    Debug.LogError("GetRegistries Failed");
                }

                onComplete(null);
            }
        } 
    }