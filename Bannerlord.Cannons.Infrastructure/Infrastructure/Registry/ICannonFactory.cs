using System;

namespace Bannerlord.Cannons.Infrastructure.Registry
{
    public interface ICannonFactory
    {
        Type CannonScriptType { get; }
    }
}
