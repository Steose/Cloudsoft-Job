using Cloudsoft.Core.Models;
using MongoDB.Bson.Serialization;

namespace Cloudsoft.Core.Repositories;

internal static class MongoDbMappings
{
    private static readonly Lock RegistrationLock = new();
    private static bool _registered;

    public static void Register()
    {
        lock (RegistrationLock)
        {
            if (_registered)
            {
                return;
            }

            RegisterClassMap<EmployerAccount>();
            RegisterClassMap<EmployerUser>();
            RegisterClassMap<JobPosting>();
            RegisterClassMap<JobApplication>();

            _registered = true;
        }
    }

    private static void RegisterClassMap<TClass>()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TClass)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<TClass>(classMap =>
        {
            classMap.AutoMap();
            classMap.SetIgnoreExtraElements(true);
        });
    }
}
