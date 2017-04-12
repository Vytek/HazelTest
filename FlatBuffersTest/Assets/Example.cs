using UnityEngine;
using System.Collections;
using System.IO;
using FlatBuffers;
using CompanyNamespaceWhatever;
using System;

public class Example : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Save();
        Load();
    }


    // Update is called once per frame
    void Save () {
        // Create flatbuffer class
        FlatBufferBuilder fbb = new FlatBufferBuilder(1);

        // Create our sword for GameDataWhatever
        //------------------------------------------------------
        
        WeaponClassesOrWhatever weaponType = WeaponClassesOrWhatever.Sword;
        Sword.StartSword(fbb);
        Sword.AddDamage(fbb, 123);
        Sword.AddDistance(fbb, 999);
        Offset<Sword> offsetWeapon = Sword.EndSword(fbb);
        
        /*
        // For gun uncomment this one and remove the sword one
        WeaponClassesOrWhatever weaponType = WeaponClassesOrWhatever.Gun;
        Gun.StartGun(fbb);
        Gun.AddDamage(fbb, 123);
        Gun.AddReloadspeed(fbb, 999);
        Offset<Gun> offsetWeapon = Gun.EndGun(fbb);
        */
        //------------------------------------------------------

        // Create strings for GameDataWhatever
        //------------------------------------------------------
        StringOffset cname = fbb.CreateString("Test String ! time : " + DateTime.Now);
        //------------------------------------------------------

        // Create GameDataWhatever object we will store string and weapon in
        //------------------------------------------------------
        GameDataWhatever.StartGameDataWhatever(fbb);

        GameDataWhatever.AddName(fbb, cname);
        GameDataWhatever.AddPos(fbb, Vec3.CreateVec3(fbb, 1, 2, 1)); // structs can be inserted directly, no need to be defined earlier
        GameDataWhatever.AddColor(fbb, CompanyNamespaceWhatever.Color.Red);

        //Store weapon
        GameDataWhatever.AddWeaponType(fbb, weaponType);
        GameDataWhatever.AddWeapon(fbb, offsetWeapon.Value);

        var offset = GameDataWhatever.EndGameDataWhatever(fbb);
        //------------------------------------------------------

        GameDataWhatever.FinishGameDataWhateverBuffer(fbb, offset);

        // Save the data into "SAVE_FILENAME.whatever" file, name doesn't matter obviously
        using (var ms = new MemoryStream(fbb.DataBuffer.Data, fbb.DataBuffer.Position, fbb.Offset)) {
            File.WriteAllBytes("SAVE_FILENAME.whatever", ms.ToArray());
            Debug.Log("SAVED !");
        }
    }

    void Load() {

        if (!File.Exists("SAVE_FILENAME.whatever")) throw new Exception("Load failed : 'SAVE_FILENAME.whatever' not exis, something went wrong");

        ByteBuffer bb = new ByteBuffer(File.ReadAllBytes("SAVE_FILENAME.whatever"));

        if (!GameDataWhatever.GameDataWhateverBufferHasIdentifier(bb)) {
            throw new Exception("Identifier test failed, you sure the identifier is identical to the generated schema's one?");
        }

        GameDataWhatever data = GameDataWhatever.GetRootAsGameDataWhatever(bb);

        Debug.Log("LOADED DATA : ");
        Debug.Log("NAME : " + data.Name);
        Debug.Log("POS : " + data.Pos.X + ", " + data.Pos.Y + ", " + data.Pos.Z);
        Debug.Log("COLOR : " + data.Color);

        Debug.Log("WEAPON TYPE : " + data.WeaponType);

        switch (data.WeaponType) {
            case WeaponClassesOrWhatever.Sword:
                Sword sword = new Sword();
                data.GetWeapon<Sword>(sword);
                Debug.Log("SWORD DAMAGE  : " + sword.Damage);
                break;
            case WeaponClassesOrWhatever.Gun:
                Gun gun = new Gun();
                data.GetWeapon<Gun>(gun);
                Debug.Log("GUN RELOAD SPEED  : " + gun.Reloadspeed);
                break;
            default:
                break;
        }
    }
}
