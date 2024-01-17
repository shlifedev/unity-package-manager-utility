using System; 
using System.Linq;
using System.Reflection; 
using Cysharp.Threading.Tasks; 
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine; 

 
namespace LD.Editor
{
    public static class PackageManagerUtility
    {

        /// <summary>
        /// Add Dependency
        /// </summary>
        /// <param name="missing">Missing Dependency</param>
        public static async UniTask AddDependency(MissingDependency missing)
        {
            bool added = false;
            foreach (var (version, index) in missing.Versions.Select((item, index) => (item, index)))
            {
                var retry = 1;
                while (retry > 0)
                {
                    retry--; 
                    var registires = await PackageManagerUtility.GetRegistries();

                    var openupmRegistry = registires.FirstOrDefault(x => x.name == "openupm");
                    var add = Client.Add(version);
                    await UniTask.WaitUntil(() => add.IsCompleted); 
                    if (add.Error != null)
                    {
                        Debug.LogErrorFormat("{0} : {1}", add.Error.errorCode, add.Error.message);
                        if (add.Error.errorCode == ErrorCode.Unknown && add.Error.message.Contains("EBUSY"))
                            retry = 3;
                    }
                    else
                    {
                        added = true;
                    } 
                    await UniTask.Yield();
                }

                if (added)
                    break;
            }
        }
        /// <summary>
        /// Get All Missing Dependencies
        /// </summary>
        /// <returns></returns>
        public static async UniTask<MissingDependency[]> GetMissingDependency()
        {
            var list = Client.List(false, true);
            await UniTask.WaitUntil(() => list.IsCompleted);
            var findMissing = list.Result
                .SelectMany(info =>
                {
                    return info.dependencies.Where(dep => !list.Result.Any(item => item.name == dep.name));
                }).GroupBy(info => info.name, info => info.version).Where(group => group.Any()).Select(group =>
                {
                    return new MissingDependency
                    {
                        Name = group.Key,
                        Versions = group.ToArray()
                    };
                }).ToArray();
            return findMissing;
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

        /// <summary>
        /// You can get already exist registries
        /// </summary> 
        public static async UniTask<string[]> GetScopes(string registryName)
        {
            var registires = await PackageManagerUtility.GetRegistries(); 
            var registry = registires.FirstOrDefault(x => x.name == registryName);
            var scopes = Getter<string[]>((registry.GetType(), "scopes", BindingFlags.Instance | BindingFlags.NonPublic), registires);
            if (scopes == null) throw new Exception("GetScopes Cannot Found Scopes");
            return scopes; 
        }

        /// <summary>
        /// Update Exists Scoped Registry.
        /// </summary>
        /// <param name="registryName">update registry name</param>
        /// <param name="url">url</param>
        /// <param name="scopes">scopes</param>
        /// <returns></returns>
        public static async UniTask<StatusCode> UpdateScopedRegistry(string registryName, string url, string[] scopes)
        {
            var id = await GetRegistryId(registryName);
            Assembly assembly = Assembly.Load("UnityEditor.CoreModule");
            Type type = assembly.GetType("UnityEditor.PackageManager.UpdateScopedRegistryOptions", true);
            object option = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[]
                {
                    registryName,
                    url,
                    scopes
                }, null);
            var request = CallMethod<Request<RegistryInfo>>("UpdateScopedRegistry", null,
                BindingFlags.NonPublic | BindingFlags.Static,
                new object[] { id, option });
            if (request != null)
            {
                await UniTask.WaitUntil(() => request.IsCompleted);
                return request.Status;
            }

            return StatusCode.Failure;
        }


        /// <summary>
        /// Add Scope Registry. Warning : if you need update scopes using UpdateScopeAndRegistry Method.
        /// </summary>
        /// <param name="registryName">registry name</param>
        /// <param name="url">url</param>
        /// <param name="scopes">scopes</param>
        /// <returns></returns>
        public static async UniTask<StatusCode> AddScopeAndRegistry(string registryName, string url, string[] scopes)
        {
            var request = CallMethod<Request<RegistryInfo>>("AddScopedRegistry", null,
                BindingFlags.NonPublic | BindingFlags.Static,
                new object[] { registryName, url, scopes });
            if (request != null)
            {
                await UniTask.WaitUntil(() => request.IsCompleted);
                if (request.Status == StatusCode.Failure)
                    Debug.LogError("Package registration failed");
                else if (request.Status == StatusCode.Success)
                {
                    var result = Getter<RegistryInfo>((typeof(Request<RegistryInfo>), "Result", BindingFlags.Default),
                        request);
                }
            }

            return request.Status;
        }


        /// <summary>
        /// Get Registry Id
        /// </summary>
        /// <param name="registryName"></param>
        /// <returns></returns>
        public static async UniTask<string> GetRegistryId(string registryName)
        {
            var regs = await GetRegistries();
            foreach (var result in regs)
            {
                if (result.name != registryName) continue;
                var properties = result.GetType()
                    .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var idProp = properties.First(x => x.Name == "id");
                var regId = idProp.GetValue(result).ToString();
                return regId;
            }

            return null;
        }

        /// <summary>
        /// Get Registries.
        /// </summary>
        /// <returns></returns>
        public static async UniTask<RegistryInfo[]> GetRegistries()
        {
            var obj = CallMethod("GetRegistries", null, BindingFlags.NonPublic | BindingFlags.Static, null);

            if (obj != null)
            {
                var request = obj as Request;
                await UniTask.WaitUntil(() => request.IsCompleted);
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
                            return Result;
                        }
                    }

                }
            }
            else
            {
                Debug.LogError("GetRegistries Failed");
            }

            return null;
        }

        #region utilities

        private static MethodInfo GetMethod(string name, BindingFlags flags)
        {
            var clientRefl = typeof(Client);
            var methods = clientRefl.GetMethods(flags);
            var methodInfo =
                methods.First(x => x.Name == name && x.ReturnType.IsClass);
            return methodInfo;
        }

        private static Request<T> CallMethod<T>(string methodName,  object instance, BindingFlags flags,
            params object[] parameters)
        {
            return Call<T>(GetMethod(methodName, flags), instance, parameters);
        }

        private static Request<T> Call<T>(MethodInfo info,  object instance, params object[] parameters)
        {
            return info.Invoke(instance, parameters: parameters) as Request<T>;
        }

        private static object CallMethod(string methodName, object instance, BindingFlags flags,
            params object[] parameters)
        {
            return Call(GetMethod(methodName, flags), instance, parameters);
        }

        private static object Call(MethodInfo info,  object instance, params object[] parameters)
        {
            return info.Invoke(instance, parameters: parameters);
        }

        private static T Getter<T>(PropertyInfo info, object instance, params object[] parameters)
        {
            return (T)info.GetValue(instance, parameters);
        }

        private static T Getter<T>((Type type, string propertyName, BindingFlags flags) propertyData,
           object instance, params object[] parameters)
        {
            var property = propertyData.type.GetProperty(propertyData.propertyName, propertyData.flags);
            return (T)property?.GetValue(instance, parameters);
        }

        private static object Getter((Type type, string propertyName, BindingFlags flags) propertyData,
            object instance, params object[] parameters)
        {
            var property = propertyData.type.GetProperty(propertyData.propertyName, propertyData.flags);
            return property?.GetValue(instance, parameters);
        }

        #endregion
    } 
}