using System.Linq;
using System.Reflection;

namespace Documentation
{
    public class Specifier<T> : ISpecifier
    {
        public string GetApiDescription()
        {
            return typeof(T).GetCustomAttributes().OfType<ApiDescriptionAttribute>().FirstOrDefault()?.Description;
        }

        public string[] GetApiMethodNames()
        {
            return typeof(T)
                .GetMethods()
                .Where(m => m.GetCustomAttributes().OfType<ApiMethodAttribute>().Any())
                .Select(k => k.Name).ToArray();
        }

        public string GetApiMethodDescription(string methodName)
        {
            return typeof(T)
                .GetMethods().FirstOrDefault(m => m.Name == methodName)
                ?.GetCustomAttributes().OfType<ApiDescriptionAttribute>().FirstOrDefault()
                ?.Description;
        }

        public string[] GetApiMethodParamNames(string methodName)
        {
            return typeof(T).GetMethods().FirstOrDefault(m => m.Name == methodName)
                ?.GetParameters().Select(p => p.Name).ToArray();
        }

        public string GetApiMethodParamDescription(string methodName, string paramName)
        {
            return typeof(T)
                .GetMethods().FirstOrDefault(m => m.Name == methodName)
                ?.GetParameters().FirstOrDefault(p => p.Name == paramName)
                ?.GetCustomAttributes().OfType<ApiDescriptionAttribute>().FirstOrDefault()
                ?.Description;
        }

        public ApiParamDescription GetApiMethodParamFullDescription(string methodName, string paramName)
        {
            var methodInfo = typeof(T).GetMethods().FirstOrDefault(m => m.Name == methodName);
            
            var parameterInfo = typeof(T)
                .GetMethods().FirstOrDefault(m => m.Name == methodName)
                ?.GetParameters().FirstOrDefault(p => p.Name == paramName);
            
            // если такого метода не существует
            if (methodInfo == null)
                return new ApiParamDescription {ParamDescription = new CommonDescription(paramName)};
            
            // если такого параметра не существует
            if (parameterInfo == null)
                return new ApiParamDescription {ParamDescription = new CommonDescription(paramName)};
            
            return GetParamDescriptions(new[] {parameterInfo})[0];
        }

        public ApiMethodDescription GetApiMethodFullDescription(string methodName)
        {
            var methodInfo = typeof(T).GetMethods().FirstOrDefault(m => m.Name == methodName);
            var parameterInfos = methodInfo?.GetParameters();

            // если метод не содержит атрибута [ApiMethod]
            if (!(methodInfo?.GetCustomAttributes().OfType<ApiMethodAttribute>()).Any())
                return null;

            var methodDescription = new CommonDescription(methodName, GetApiMethodDescription(methodName));
            var paramDescriptions = GetParamDescriptions(parameterInfos);

            // если метод имееет тип void
            if (methodInfo?.ReturnType.FullName == "System.Void")
                return new ApiMethodDescription
                {
                    MethodDescription = methodDescription,
                    ParamDescriptions = paramDescriptions
                };

            var returnDescription = GetReturnDescription(methodInfo);

            return new ApiMethodDescription
            {
                MethodDescription = methodDescription,
                ParamDescriptions = paramDescriptions,
                ReturnDescription = returnDescription
            };
        }
        
        private static ApiParamDescription[] GetParamDescriptions(ParameterInfo[] parameterInfos)
        {
            var paramDescriptions = new ApiParamDescription[parameterInfos.Length];
            
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var isRequiredAttribute = parameterInfos[i].GetCustomAttributes().OfType<ApiRequiredAttribute>().Any();
                var requiredAttribute = parameterInfos[i].GetCustomAttributes().OfType<ApiRequiredAttribute>()
                    .FirstOrDefault();
                
                var apiDescription = parameterInfos[i].GetCustomAttributes().OfType<ApiDescriptionAttribute>()
                    .FirstOrDefault()?.Description;

                var apiIntValidationAttribute = parameterInfos[i].GetCustomAttributes()
                    .OfType<ApiIntValidationAttribute>().FirstOrDefault();

                if (apiIntValidationAttribute == null)
                {
                    paramDescriptions[i] = new ApiParamDescription
                    {
                        ParamDescription = new CommonDescription(parameterInfos[i].Name, apiDescription),
                        Required = requiredAttribute?.Required ?? isRequiredAttribute
                    };
                }
                else
                    paramDescriptions[i] = new ApiParamDescription
                    {
                        ParamDescription = new CommonDescription(parameterInfos[i].Name, apiDescription),
                        Required = requiredAttribute?.Required ?? isRequiredAttribute,
                        MinValue = apiIntValidationAttribute.MinValue,
                        MaxValue = apiIntValidationAttribute.MaxValue
                    };
            }
            return paramDescriptions;
        }
        
        private static ApiParamDescription GetReturnDescription(MethodInfo methodInfo)
        {
            var apiRequiredAttribute = methodInfo
                ?.ReturnTypeCustomAttributes
                .GetCustomAttributes(false).OfType<ApiRequiredAttribute>().FirstOrDefault();

            var apiIntValidationAttribute = methodInfo
                ?.ReturnTypeCustomAttributes
                .GetCustomAttributes(false).OfType<ApiIntValidationAttribute>().FirstOrDefault();

            var returnDescription = new ApiParamDescription();

            if (apiIntValidationAttribute != null && apiRequiredAttribute != null)
            {
                returnDescription = new ApiParamDescription
                {
                    Required = apiRequiredAttribute.Required,
                    ParamDescription = new CommonDescription(),
                    MinValue = apiIntValidationAttribute.MinValue,
                    MaxValue = apiIntValidationAttribute.MaxValue
                };
            }
            return returnDescription;
        }
    }
}