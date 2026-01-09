using System;
using Newtonsoft.Json;

public class PRJsonUtils 
{
    public static bool TryDeserializeObject<T>(string json, out T result, bool showError = true)
    {
        result = default;
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            PRLog.WriteDebug(typeof(PRJsonUtils), "TryLoadData");
            PRLog.WriteDebug(typeof(PRJsonUtils), json);

            result = JsonConvert.DeserializeObject<T>(json, GetJsonYandexSettings(showError));
            PRLog.WriteDebug(typeof(PRJsonUtils), "EndLoad");

            return result != null;
        }
        catch (JsonException ex)
        {
            if(showError)
                PRLog.WriteDebug(typeof(PRJsonUtils), ex.ToString()); 

            return false;
        }
    }

    public static JsonSerializerSettings GetJsonYandexSettings(bool showError = true)
    {
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore, // Игнорировать неизвестные поля
            NullValueHandling = NullValueHandling.Include, // Включать null
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Error = (sender, args) =>
            {
                if(showError)
                    PRLog.WriteWarning(typeof(PRJsonUtils), $"Ошибка десериализации: {args.ErrorContext.Error.Message} Путь: {args.ErrorContext.Path}");

                args.ErrorContext.Handled = true; // Игнорировать ошибку и продолжить
            }
        };
        return settings;
    }

    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj, GetJsonYandexSettings());
    }


    /// <summary>
    /// Шифрует объект в строку JSON и затем применяет AES-256.
    /// </summary>
    public static string SerializeObjectWithEncryption(object obj)
    {
        string json = SerializeObject(obj);
        return PRCrypto.EncryptString(json, PRCrypto.Password);
    }

    /// <summary>
    /// Расшифровывает AES-256 строку в JSON и десериализует в объект.
    /// </summary>
    public static T Decrypt<T>(string encryptedJson)
    {
        string json = PRCrypto.DecryptString(encryptedJson, PRCrypto.Password);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// Расшифровывает AES-256 строку в JSON и десериализует в объект.
    /// </summary>
    public static bool TryDeserializeObjectDecrypt<T>(string encryptedJson, out T result)
    {
        result = default(T);
        try
        {
            string json = PRCrypto.DecryptString(encryptedJson, PRCrypto.Password);
            return TryDeserializeObject<T>(json, out result);
        }
        catch(Exception ex)
        {
            PRLog.WriteWarning(typeof(PRJsonUtils), ex.ToString());
            return false;
        }
    }
}
 