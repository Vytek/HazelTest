// automatically generated, do not modify

namespace CompanyNamespaceWhatever
{

using FlatBuffers;

public enum Color : sbyte
{
 Red = 1,
 Green = 2,
 Blue = 3,
};

public enum WeaponClassesOrWhatever : byte
{
 NONE = 0,
 Sword = 1,
 Gun = 2,
};

public sealed class Vec3 : Struct {
  public Vec3 __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public float X { get { return bb.GetFloat(bb_pos + 0); } }
  public float Y { get { return bb.GetFloat(bb_pos + 4); } }
  public float Z { get { return bb.GetFloat(bb_pos + 8); } }

  public static Offset<Vec3> CreateVec3(FlatBufferBuilder builder, float X, float Y, float Z) {
    builder.Prep(4, 12);
    builder.PutFloat(Z);
    builder.PutFloat(Y);
    builder.PutFloat(X);
    return new Offset<Vec3>(builder.Offset);
  }
};

public sealed class GameDataWhatever : Table {
  public static GameDataWhatever GetRootAsGameDataWhatever(ByteBuffer _bb) { return GetRootAsGameDataWhatever(_bb, new GameDataWhatever()); }
  public static GameDataWhatever GetRootAsGameDataWhatever(ByteBuffer _bb, GameDataWhatever obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public static bool GameDataWhateverBufferHasIdentifier(ByteBuffer _bb) { return __has_identifier(_bb, "WHAT"); }
  public GameDataWhatever __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public Vec3 Pos { get { return GetPos(new Vec3()); } }
  public Vec3 GetPos(Vec3 obj) { int o = __offset(4); return o != 0 ? obj.__init(o + bb_pos, bb) : null; }
  public short Mana { get { int o = __offset(6); return o != 0 ? bb.GetShort(o + bb_pos) : (short)150; } }
  public short Hp { get { int o = __offset(8); return o != 0 ? bb.GetShort(o + bb_pos) : (short)100; } }
  public string Name { get { int o = __offset(10); return o != 0 ? __string(o + bb_pos) : null; } }
  public byte GetInventory(int j) { int o = __offset(12); return o != 0 ? bb.Get(__vector(o) + j * 1) : (byte)0; }
  public int InventoryLength { get { int o = __offset(12); return o != 0 ? __vector_len(o) : 0; } }
  public Color Color { get { int o = __offset(14); return o != 0 ? (Color)bb.GetSbyte(o + bb_pos) : (Color)3; } }
  public WeaponClassesOrWhatever WeaponType { get { int o = __offset(16); return o != 0 ? (WeaponClassesOrWhatever)bb.Get(o + bb_pos) : (WeaponClassesOrWhatever)0; } }
  public TTable GetWeapon<TTable>(TTable obj) where TTable : Table { int o = __offset(18); return o != 0 ? __union(obj, o) : null; }

  public static void StartGameDataWhatever(FlatBufferBuilder builder) { builder.StartObject(8); }
  public static void AddPos(FlatBufferBuilder builder, Offset<Vec3> posOffset) { builder.AddStruct(0, posOffset.Value, 0); }
  public static void AddMana(FlatBufferBuilder builder, short mana) { builder.AddShort(1, mana, 150); }
  public static void AddHp(FlatBufferBuilder builder, short hp) { builder.AddShort(2, hp, 100); }
  public static void AddName(FlatBufferBuilder builder, StringOffset nameOffset) { builder.AddOffset(3, nameOffset.Value, 0); }
  public static void AddInventory(FlatBufferBuilder builder, VectorOffset inventoryOffset) { builder.AddOffset(4, inventoryOffset.Value, 0); }
  public static VectorOffset CreateInventoryVector(FlatBufferBuilder builder, byte[] data) { builder.StartVector(1, data.Length, 1); for (int i = data.Length - 1; i >= 0; i--) builder.AddByte(data[i]); return builder.EndVector(); }
  public static void StartInventoryVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(1, numElems, 1); }
  public static void AddColor(FlatBufferBuilder builder, Color color) { builder.AddSbyte(5, (sbyte)(color), 3); }
  public static void AddWeaponType(FlatBufferBuilder builder, WeaponClassesOrWhatever weaponType) { builder.AddByte(6, (byte)(weaponType), 0); }
  public static void AddWeapon(FlatBufferBuilder builder, int weaponOffset) { builder.AddOffset(7, weaponOffset, 0); }
  public static Offset<GameDataWhatever> EndGameDataWhatever(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<GameDataWhatever>(o);
  }
  public static void FinishGameDataWhateverBuffer(FlatBufferBuilder builder, Offset<GameDataWhatever> offset) { builder.Finish(offset.Value, "WHAT"); }
};

public sealed class Sword : Table {
  public static Sword GetRootAsSword(ByteBuffer _bb) { return GetRootAsSword(_bb, new Sword()); }
  public static Sword GetRootAsSword(ByteBuffer _bb, Sword obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public Sword __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public int Damage { get { int o = __offset(4); return o != 0 ? bb.GetInt(o + bb_pos) : (int)10; } }
  public short Distance { get { int o = __offset(6); return o != 0 ? bb.GetShort(o + bb_pos) : (short)5; } }

  public static Offset<Sword> CreateSword(FlatBufferBuilder builder,
      int damage = 10,
      short distance = 5) {
    builder.StartObject(2);
    Sword.AddDamage(builder, damage);
    Sword.AddDistance(builder, distance);
    return Sword.EndSword(builder);
  }

  public static void StartSword(FlatBufferBuilder builder) { builder.StartObject(2); }
  public static void AddDamage(FlatBufferBuilder builder, int damage) { builder.AddInt(0, damage, 10); }
  public static void AddDistance(FlatBufferBuilder builder, short distance) { builder.AddShort(1, distance, 5); }
  public static Offset<Sword> EndSword(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<Sword>(o);
  }
};

public sealed class Gun : Table {
  public static Gun GetRootAsGun(ByteBuffer _bb) { return GetRootAsGun(_bb, new Gun()); }
  public static Gun GetRootAsGun(ByteBuffer _bb, Gun obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public Gun __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public int Damage { get { int o = __offset(4); return o != 0 ? bb.GetInt(o + bb_pos) : (int)500; } }
  public short Reloadspeed { get { int o = __offset(6); return o != 0 ? bb.GetShort(o + bb_pos) : (short)2; } }

  public static Offset<Gun> CreateGun(FlatBufferBuilder builder,
      int damage = 500,
      short reloadspeed = 2) {
    builder.StartObject(2);
    Gun.AddDamage(builder, damage);
    Gun.AddReloadspeed(builder, reloadspeed);
    return Gun.EndGun(builder);
  }

  public static void StartGun(FlatBufferBuilder builder) { builder.StartObject(2); }
  public static void AddDamage(FlatBufferBuilder builder, int damage) { builder.AddInt(0, damage, 500); }
  public static void AddReloadspeed(FlatBufferBuilder builder, short reloadspeed) { builder.AddShort(1, reloadspeed, 2); }
  public static Offset<Gun> EndGun(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<Gun>(o);
  }
};


}
