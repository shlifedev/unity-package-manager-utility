using System; 
using System.Linq;
using System.Reflection;   
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests; 
namespace LD.Editor
{
    public static partial class PackageManagerUtility
    {
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