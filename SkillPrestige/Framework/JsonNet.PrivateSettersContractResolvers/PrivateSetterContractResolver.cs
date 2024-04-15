using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace SkillPrestige.Framework.JsonNet.PrivateSettersContractResolvers
{
    /// <summary>Json Contract resolver to set values of private fields when deserializing Json data.</summary>
    internal class PrivateSetterContractResolver : DefaultContractResolver
    {
        /*********
        ** Protected methods
        *********/
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.Writable)
                return property;

            property.Writable = member.IsPropertyWithSetter();

            return property;
        }
    }
}
