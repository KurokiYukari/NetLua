namespace NetLua.Interop;

public interface ILuaInteropService
{
    LuaObject Index(ILuaInteropContext context, LuaObject index);
}

public interface ILuaInteropContext
{
    LuaObject Owner { get; }
    object Target { get; }
}
