#pragma warning disable CA1859 // Use concrete types when possible for improved performance -- most of prototype methods return JsValue

using Jint.Native.Object;
using Jint.Native.Symbol;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.WeakMap;

/// <summary>
/// https://tc39.es/ecma262/#sec-weakmap-objects
/// </summary>
internal sealed class WeakMapPrototype : Prototype
{
    private readonly WeakMapConstructor _constructor;

    internal WeakMapPrototype(
        Engine engine,
        Realm realm,
        WeakMapConstructor constructor,
        ObjectPrototype prototype) : base(engine, realm)
    {
        _prototype = prototype;
        _constructor = constructor;
    }

    protected override void Initialize()
    {
        const PropertyFlag PropertyFlags = PropertyFlag.Configurable | PropertyFlag.Writable;
        var properties = new PropertyDictionary(8, checkExistingKeys: false)
        {
            ["length"] = new PropertyDescriptor(0, PropertyFlag.Configurable),
            ["constructor"] = new PropertyDescriptor(_constructor, PropertyFlag.NonEnumerable),
            ["delete"] = new PropertyDescriptor(new ClrFunction(Engine, "delete", Delete, 1, PropertyFlag.Configurable), PropertyFlags),
            ["get"] = new PropertyDescriptor(new ClrFunction(Engine, "get", Get, 1, PropertyFlag.Configurable), PropertyFlags),
            ["getOrInsert"] = new PropertyDescriptor(new ClrFunction(Engine, "getOrInsert", GetOrInsert, 2, PropertyFlag.Configurable), PropertyFlags),
            ["getOrInsertComputed"] = new PropertyDescriptor(new ClrFunction(Engine, "getOrInsertComputed", GetOrInsertComputed, 2, PropertyFlag.Configurable), PropertyFlags),
            ["has"] = new PropertyDescriptor(new ClrFunction(Engine, "has", Has, 1, PropertyFlag.Configurable), PropertyFlags),
            ["set"] = new PropertyDescriptor(new ClrFunction(Engine, "set", Set, 2, PropertyFlag.Configurable), PropertyFlags),
        };
        SetProperties(properties);

        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.ToStringTag] = new PropertyDescriptor("WeakMap", false, false, true)
        };
        SetSymbols(symbols);
    }

    private JsValue Get(JsValue thisObject, JsCallArguments arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        return map.WeakMapGet(arguments.At(0));
    }

    private JsValue GetOrInsert(JsValue thisObject, JsCallArguments arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        var key = AssertCanBeHeldWeakly(arguments.At(0));
        var value = arguments.At(1);
        return map.GetOrInsert(key, value);
    }

    private JsValue GetOrInsertComputed(JsValue thisObject, JsCallArguments arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        var key = AssertCanBeHeldWeakly(arguments.At(0));
        var callbackfn = arguments.At(1).GetCallable(_realm);
        return map.GetOrInsertComputed(key, callbackfn);
    }

    private JsValue AssertCanBeHeldWeakly(JsValue key)
    {
        if (!key.CanBeHeldWeakly(_engine.GlobalSymbolRegistry))
        {
            Throw.TypeError(_realm);
        }

        return key;
    }

    private JsValue Delete(JsValue thisObject, JsCallArguments arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        return arguments.Length > 0 && map.WeakMapDelete(arguments.At(0)) ? JsBoolean.True : JsBoolean.False;
    }

    private JsValue Set(JsValue thisObject, JsCallArguments arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        map.WeakMapSet(arguments.At(0), arguments.At(1));
        return thisObject;
    }

    private JsValue Has(JsValue thisObject, JsCallArguments arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        return map.WeakMapHas(arguments.At(0)) ? JsBoolean.True : JsBoolean.False;
    }

    private JsWeakMap AssertWeakMapInstance(JsValue thisObject)
    {
        if (thisObject is JsWeakMap map)
        {
            return map;
        }

        Throw.TypeError(_realm, "object must be a WeakMap");
        return default;
    }
}
