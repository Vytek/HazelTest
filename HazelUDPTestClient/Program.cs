using System;
using System.Net;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatBuffers;

using Hazel;
using Hazel.Udp;

using HazelTest;
using HazelMessage;
using System.IO;

namespace HazelUDPTestClient
{
	class ClientExample 
    {
        /// <summary>
        /// Send type.
        /// </summary>
        public enum SendType : byte
        {
            SENDTOALL = 0,
            SENDTOOTHER = 1,
            SENDTOSERVER = 2,
            SENDTOUID = 3 //NOT IMPLEMENTED
        }

        /// <summary>
        /// Packet identifier.
        /// </summary>
        public enum PacketId : sbyte
        {
            PLAYER_JOIN = 0,
            OBJECT_MOVE = 1,
            PLAYER_SPAWN = 2,
            OBJECT_SPAWN = 3,
            PLAYER_MOVE = 4,
            MESSAGE_SERVER = 5
        }

        /// <summary>
        /// Command type.
        /// </summary>
        public enum CommandType : sbyte
        {
            LOGIN = 0,
            DISCONNECTEDCLIENT = 1
        }

        static Connection connection;
        static Boolean DEBUG = true;
        static String UID = String.Empty;
        static String AvatarName = String.Empty;

        private static Vector3 lastPosition = new Vector3(0, 0, 0);
        private static Quaternion lastRotation = new Quaternion(1, 1, 1, 1);

		public static void Main(string[] args)
		{
			NetworkEndPoint endPoint = new NetworkEndPoint("127.0.0.1", 4296);

			connection = new UdpClientConnection(endPoint);

			connection.DataReceived += DataReceived;
			connection.Disconnected += ServerDisconnectHandler;

			try
			{
				Console.WriteLine("Connecting!");

				connection.Connect();

				//Send single login message
                {
                    //Create LOGIN message
                    //Login
                    String UIDBuffer = "Vytek75;test1234!"; 

                    //Encode FlatBuffer
                    //Create flatbuffer class
                    FlatBufferBuilder fbb = new FlatBufferBuilder(1);

                    StringOffset SOUIDBuffer = fbb.CreateString(UIDBuffer);

                    HazelMessage.HMessage.StartHMessage(fbb);
                    HazelMessage.HMessage.AddCommand(fbb, (sbyte)CommandType.LOGIN);
                    HazelMessage.HMessage.AddAnswer(fbb, SOUIDBuffer);
                    var offset = HazelMessage.HMessage.EndHMessage(fbb);
                    HazelMessage.HMessage.FinishHMessageBuffer(fbb, offset);
                    //Reply to Client
                    using (var ms = new MemoryStream(fbb.DataBuffer.Data, fbb.DataBuffer.Position, fbb.Offset))
                    {
                        //Add type!
                        //https://stackoverflow.com/questions/5591329/c-sharp-how-to-add-byte-to-byte-array
                        byte[] newArray = new byte[ms.ToArray().Length + 1];
                        ms.ToArray().CopyTo(newArray, 1);
                        newArray[0] = (byte)SendType.SENDTOSERVER;
                        //SEND RELIABLE UDP
                        connection.SendBytes(newArray, SendOption.Reliable);
                    }
                    Console.WriteLine("Send to: " + connection.EndPoint.ToString());
                }

                ConsoleKeyInfo cki;
                // Prevent example from ending if CTL+C is pressed.
                Console.TreatControlCAsInput = true;

                Console.WriteLine("Press any combination of CTL, ALT, and SHIFT, and a console key.");
                Console.WriteLine("Press the Escape (Esc) key to quit: \n");
                do
                {
                    cki = Console.ReadKey();
                    Console.Write(" --- You pressed ");
                    if ((cki.Modifiers & ConsoleModifiers.Alt) != 0) Console.Write("ALT+");
                    if ((cki.Modifiers & ConsoleModifiers.Shift) != 0) Console.Write("SHIFT+");
                    if ((cki.Modifiers & ConsoleModifiers.Control) != 0) Console.Write("CTL+");
                    Console.WriteLine(cki.Key.ToString());
                } while (cki.Key != ConsoleKey.Escape);

                connection.Close();
                Environment.Exit(0);

                /*
                Console.WriteLine("Press any key to continue...");
				Console.ReadKey();
				//https://msdn.microsoft.com/it-it/library/471w8d85(v=vs.110).aspx?cs-save-lang=1&cs-lang=cpp#code-snippet-3
				connection.Close();
				Environment.Exit(0);
				*/
			}
			catch (Hazel.HazelException ex)
			{
				Console.Error.WriteLine("Error: " + ex.Message + " from " + ex.Source);
				connection.Close();
				Environment.Exit(1);
			}
		}

		private static void DataReceived(object sender, DataReceivedEventArgs args)
		{
			Console.WriteLine("Received (" + string.Join<byte>(", ", args.Bytes) + ") from " + connection.EndPoint.ToString());
            //Decode parse received data
            //Remove first byte (type)
            //https://stackoverflow.com/questions/31550484/faster-code-to-remove-first-elements-from-byte-array
            byte STypeBuffer = args.Bytes[0]; //This is NOT TypeBuffer ;-)
            byte[] NewBufferReceiver = new byte[args.Bytes.Length - 1];
            Array.Copy(args.Bytes, 1, NewBufferReceiver, 0, NewBufferReceiver.Length);
            ByteBuffer bb = new ByteBuffer(NewBufferReceiver);
            if ((STypeBuffer == (byte)SendType.SENDTOALL) || (STypeBuffer == (byte)SendType.SENDTOOTHER))
            {
                HazelTest.Object ObjectReceived = HazelTest.Object.GetRootAsObject(bb);
                if (DEBUG)
                {
                    Console.WriteLine("RECEIVED DATA: ");
                    Console.WriteLine("IDObject RECEIVED: " + ObjectReceived.ID);
                    Console.WriteLine("UID RECEIVED; " + ObjectReceived.Owner);
                    Console.WriteLine("isKinematic: " + ObjectReceived.IsKine);
                    Console.WriteLine("POS RECEIVED: " + ObjectReceived.Pos.X + ", " + ObjectReceived.Pos.Y + ", " + ObjectReceived.Pos.Z);
                    Console.WriteLine("ROT RECEIVED: " + ObjectReceived.Rot.X + ", " + ObjectReceived.Rot.Y + ", " + ObjectReceived.Rot.Z + ", " + ObjectReceived.Rot.W);
                }
                //var ReceiveMessageFromGameObjectBuffer = new ReceiveMessageFromGameObject(); //NOT USED!
                sbyte TypeBuffer = ObjectReceived.Type;

                if ((byte)PacketId.PLAYER_JOIN == ObjectReceived.Type)
                {
                    Console.WriteLine("Add new Player!");
                    //Code for new Player
                    //Spawn something? YES
                    //Using Dispatcher? NO
                    //PlayerSpawn
                    SendMessage(SendType.SENDTOOTHER, PacketId.PLAYER_SPAWN, 0, UID + ";" + AvatarName, true, lastPosition, lastRotation);
                    //TO DO: Using Reliable UDP??
                }
                else if ((byte)PacketId.OBJECT_MOVE == ObjectReceived.Type)
                {
                    Console.WriteLine("OBJECT MOVE");
                }
                else if ((byte)PacketId.PLAYER_MOVE == ObjectReceived.Type)
                {
                    Console.WriteLine("PLAYER MOVE");
                }
                else if ((byte)PacketId.PLAYER_SPAWN == ObjectReceived.Type)
                {
                    Console.WriteLine("PLAYER SPAWN");
                }
                else if ((byte)PacketId.OBJECT_SPAWN == ObjectReceived.Type)
                {
                    Console.WriteLine("OBJECT SPAWN");
                }
            } else if (STypeBuffer == (byte) SendType.SENDTOSERVER)
                {
                    HazelMessage.HMessage HMessageReceived = HazelMessage.HMessage.GetRootAsHMessage(bb);
                    if ((sbyte)CommandType.LOGIN == HMessageReceived.Command)
                    {
                        if (HMessageReceived.Answer != String.Empty)
                        {
                            UID = HMessageReceived.Answer;
                            //Set UID for Your Avatar ME
                            //UnityMainThreadDispatcher.Instance().Enqueue(SetUIDInMainThread(HMessageReceived.Answer));
                            Console.WriteLine("UID RECEIVED: " + HMessageReceived.Answer);
                            //PLAYER_JOIN MESSAGE (SENDTOOTHER)
                            SendMessage(SendType.SENDTOOTHER, PacketId.PLAYER_JOIN, 0, UID + ";" + AvatarName, true, lastPosition, lastRotation);
                            //TO DO: Using Reliable UDP??
                        }
                        else
                        {
                            Console.WriteLine("UID RECEIVED is EMPTY (NOT VALID PASSWORD): " + HMessageReceived.Answer);
                            //Disconnect
                            if (connection != null)
                            {
                                Console.WriteLine("DisConnecting from: " + connection.EndPoint.ToString());
                                connection.Close();
                            }
                        }
                    }
                    else if ((sbyte)CommandType.DISCONNECTEDCLIENT == HMessageReceived.Command)
                    {
                        //Debug Disconnected UID
                        Console.WriteLine("UID RECEIVED and TO DESTROY: " + HMessageReceived.Answer);
                    }
                }
			args.Recycle();
		}

		private static void ServerDisconnectHandler(object sender, DisconnectedEventArgs args)
		{
			Connection connection = (Connection)sender;

			Console.WriteLine("Server connection at " + connection.EndPoint + " lost");

			connection = null;

			args.Recycle();
		}

        /// <summary>
        /// SendMessage
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="IDObject"></param>
        /// <param name="Pos"></param>
        /// <param name="Rot"></param>
        public static void SendMessage(SendType SType, PacketId Type, ushort IDObject, string OwnerPlayer, bool isKine, Vector3 Pos, Quaternion Rot)
        {
            sbyte TypeBuffer = 0;
            byte STypeBuffer = 0;

            //Choose who to send message
            switch (SType)
            {
                case SendType.SENDTOALL:
                    STypeBuffer = 0;
                    break;
                case SendType.SENDTOOTHER:
                    STypeBuffer = 1;
                    break;
                case SendType.SENDTOSERVER:
                    STypeBuffer = 2;
                    break;
                default:
                    STypeBuffer = 0;
                    break;
            }
            //Console.WriteLine("SENDTYPE SENT: " + STypeBuffer); //DEBUG

            //Choose type message (TO Modify)
            switch (Type)
            {
                case PacketId.PLAYER_JOIN:
                    TypeBuffer = 0;
                    break;
                case PacketId.OBJECT_MOVE:
                    TypeBuffer = 1;
                    break;
                case PacketId.PLAYER_SPAWN:
                    TypeBuffer = 2;
                    break;
                case PacketId.OBJECT_SPAWN:
                    TypeBuffer = 3;
                    break;
                case PacketId.PLAYER_MOVE:
                    TypeBuffer = 4;
                    break;
                case PacketId.MESSAGE_SERVER:
                    TypeBuffer = 5;
                    break;
                default:
                    TypeBuffer = 1;
                    break;
            }
            //Debug.Log("TYPE SENT: " + TypeBuffer); //DEBUG

            // Create flatbuffer class
            FlatBufferBuilder fbb = new FlatBufferBuilder(1);

            StringOffset SOUIDBuffer = fbb.CreateString(OwnerPlayer);

            HazelTest.Object.StartObject(fbb);
            HazelTest.Object.AddType(fbb, TypeBuffer);
            HazelTest.Object.AddOwner(fbb, SOUIDBuffer);
            HazelTest.Object.AddIsKine(fbb, isKine);
            HazelTest.Object.AddID(fbb, IDObject);
            HazelTest.Object.AddPos(fbb, Vec3.CreateVec3(fbb, Pos.X, Pos.Y, Pos.Z));
            HazelTest.Object.AddRot(fbb, Vec4.CreateVec4(fbb, Rot.X, Rot.Y, Rot.Z, Rot.W));
            if (DEBUG)
            {
                Console.WriteLine("ID SENT: " + IDObject.ToString());
                Console.WriteLine("UID SENT: " + OwnerPlayer);
                Console.WriteLine("POS SENT: " + Pos.X.ToString() + ", " + Pos.Y.ToString() + ", " + Pos.Z.ToString());
                Console.WriteLine("ROT SENT: " + Rot.X.ToString() + ", " + Rot.Y.ToString() + ", " + Rot.Z.ToString() + ", " + Rot.W.ToString());
            }
            var offset = HazelTest.Object.EndObject(fbb);

            HazelTest.Object.FinishObjectBuffer(fbb, offset);

            using (var ms = new MemoryStream(fbb.DataBuffer.Data, fbb.DataBuffer.Position, fbb.Offset))
            {
                //Add type!
                //https://stackoverflow.com/questions/5591329/c-sharp-how-to-add-byte-to-byte-array
                byte[] newArray = new byte[ms.ToArray().Length + 1];
                ms.ToArray().CopyTo(newArray, 1);
                newArray[0] = STypeBuffer;
                connection.SendBytes(newArray, SendOption.None); //WARNING: ALL MESSAGES ARE NOT RELIABLE!
                if (DEBUG)
                {
                    Console.WriteLine("Message sent!");
                }
            }
        }
	}
}