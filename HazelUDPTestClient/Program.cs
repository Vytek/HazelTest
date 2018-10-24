using System;
using System.Net;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatBuffers;
using RestSharp;

using Hazel;
using Hazel.Udp;

using HazelTest;
using HazelMessage;
using System.IO;

using IniParser;
using IniParser.Model;

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
            MESSAGE_SERVER = 5,
            OBJECT_UNSPAWN = 6
        }

        /// <summary>
        /// Command type.
        /// </summary>
        public enum CommandType : sbyte
        {
            LOGIN = 0,
            DISCONNECTEDCLIENT = 1
        }

        /// <summary>
        /// Initial configuation
        /// </summary>
        static Connection connection;
        static Boolean DEBUG = true;
        static String UID = String.Empty;
        static String AvatarName = String.Empty;
        static String AvatarPassword = String.Empty;
        static String ServerIP = String.Empty;
        static int ServerPort = 4296;
        static Dictionary<int, string> DictObjects = new Dictionary<int, string>();

        private static Vector3 lastPosition = new Vector3(0, 0, 0);
        private static Quaternion lastRotation = new Quaternion(1, 1, 1, 1);

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
		public static void Main(string[] args)
        {
            //Read Initial Config of Client
            //Create an instance of a ini file parser
            FileIniDataParser fileIniData = new FileIniDataParser();
            //Parse the ini file
            IniData parsedData = fileIniData.ReadFile("HazelUDPTestClient.ini");

            ServerIP = parsedData["ServerConfig"]["ServerIP"];
            //https://www.cambiaresearch.com/articles/68/convert-string-to-int-in-csharp
            ServerPort = System.Convert.ToInt32(parsedData["ServerConfig"]["ServerPort"]);

            Console.WriteLine("ServerIP: "+ parsedData["ServerConfig"]["ServerIP"]);
            Console.WriteLine("ServerPort: " + parsedData["ServerConfig"]["ServerPort"]);

            NetworkEndPoint endPoint = new NetworkEndPoint(ServerIP, ServerPort, IPMode.IPv4);

            connection = new UdpClientConnection(endPoint);

            connection.DataReceived += DataReceived;
            connection.Disconnected += ServerDisconnectHandler;

            try
            {
                Console.WriteLine("Connecting!");
                connection.Connect();

                //Send single login message
                {
                    //Login
                    //AvatarName = "Vytek75";
                    AvatarName = parsedData["User"]["UserLogin"];
                    //AvatarPassword = "test1234!";
                    AvatarPassword = parsedData["User"]["UserPassword"];
                    SendMessageToServer((sbyte)CommandType.LOGIN);
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
                    if (cki.Key.ToString().ToLower() == "r") {
                        //https://gateway.ipfs.io/ipfs/QmerSDvd9PTgcbTAz68rL1ujeCZakhdLeAUpdcfdkyhqyx
                        RezObject("QmerSDvd9PTgcbTAz68rL1ujeCZakhdLeAUpdcfdkyhqyx", UID, true);
                    }
                    if (cki.Key.ToString().ToLower() == "u")
                    {
                        //https://gateway.ipfs.io/ipfs/QmerSDvd9PTgcbTAz68rL1ujeCZakhdLeAUpdcfdkyhqyx
                        DeRezObject("QmerSDvd9PTgcbTAz68rL1ujeCZakhdLeAUpdcfdkyhqyx", UID, true, (ushort)DictObjects.Count);
                    }
                } while (cki.Key != ConsoleKey.Escape);

                connection.Close();
                Environment.Exit(0);
            }
            catch (Hazel.HazelException ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message + " from " + ex.Source);
                connection.Close();
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Datas the received.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
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
                    //Rez Object Recieved
                    RezObject(ObjectReceived.Owner.Split(';')[1], ObjectReceived.Owner.Split(';')[0], false);
                }
                else if ((byte)PacketId.OBJECT_UNSPAWN == ObjectReceived.Type)
                {
                    Console.WriteLine("OBJECT UNSPAWN");
                    //De Rez Object Recieved
                    DeRezObject(ObjectReceived.Owner.Split(';')[1], ObjectReceived.Owner.Split(';')[0], false, ObjectReceived.ID);
                }
            }
            else if (STypeBuffer == (byte)SendType.SENDTOSERVER)
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

        /// <summary>
        /// Servers the disconnect handler.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
		private static void ServerDisconnectHandler(object sender, DisconnectedEventArgs args)
        {
            Connection connection = (Connection)sender;
            Console.WriteLine("Server connection at " + connection.EndPoint + " lost");
            connection = null;
            args.Recycle();
        }

        /// <summary>
        /// Rezs the object.
        /// </summary>
        /// <param name="ObjectHASHIPFS">Object hashipfs.</param>
        /// <param name="Request">If set to <c>true</c> request.</param>
        private static void RezObject(String ObjectHASHIPFS, String vUID, Boolean Request)
        {
            Console.WriteLine("IPFS Object hash: "+ObjectHASHIPFS);
            var client = new RestClient();
            client.BaseUrl = new Uri("http://localhost:5001/api/");
            var request = new RestRequest("v0/id", Method.GET);
            //request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
            //request.AddUrlSegment("id", "1"); // replaces matching token in request.Resource
            // execute the request
            IRestResponse response = client.Execute(request);
            if (!String.IsNullOrEmpty(response.Content)) {
                var content = response.Content; // raw content as string
                var contenttype = response.ContentType;
                Console.WriteLine(content);
                Console.WriteLine(contenttype);
                //Request file from IPFS
                var request_ipfs = new RestRequest("v0/cat?arg="+ObjectHASHIPFS, Method.GET);
                IRestResponse response_ipfs = client.Execute(request_ipfs);
                //Instance it in Unity3D Engine
                if (!String.IsNullOrEmpty(response_ipfs.Content)) {
                    var content_ipfs = response_ipfs.Content; // raw content as string
                    var contenttype_ipfs = response_ipfs.ContentType;
                    Console.WriteLine(content_ipfs);
                    Console.WriteLine(contenttype_ipfs);
                    //Add Object to internal list in client
                    ushort UIDObject;
                    {
                        UIDObject = (ushort)(DictObjects.Count + 1);
                    }
                    DictObjects.Add(UIDObject, AvatarName + ";" + ObjectHASHIPFS);
                    if (Request)
                    {
                        //Send command to rez in others clients
                        SendMessage(SendType.SENDTOOTHER, PacketId.OBJECT_SPAWN, UIDObject, AvatarName + ";" + ObjectHASHIPFS, true, lastPosition, lastRotation);
                    }
                } else {
                    Console.WriteLine("ERROR: IPFS hash NOT correct or other problem!");  
                }                    
            } else {
                Console.WriteLine("ERROR: IPFS is NOT active!");
            }

        }

        /// <summary>
        /// Des the rez object.
        /// </summary>
        /// <param name="ObjectHASHIPFS">Object hashipfs.</param>
        /// <param name="Request">If set to <c>true</c> request.</param>
        private static void DeRezObject(String ObjectHASHIPFS, String vUID, Boolean Request, ushort UIDObject)
        {
            if (DictObjects.Count != 0)
            {
                if (DictObjects.Contains(new KeyValuePair<int, string>(UIDObject, AvatarName+";"+ObjectHASHIPFS)))
                {
                    Console.WriteLine("UID: "+vUID.ToString());
                    Console.WriteLine("UIDOject: " + UIDObject.ToString());
                    Console.WriteLine("HASHIPFS: "+ObjectHASHIPFS);
                    //Remove Object/Remove GameObject
                    DictObjects.Remove(UIDObject);
                    if (Request)
                    {
                        //Send command to rez in others clients
                        SendMessage(SendType.SENDTOOTHER, PacketId.OBJECT_UNSPAWN, UIDObject, AvatarName + ";" + ObjectHASHIPFS, true, lastPosition, lastRotation);
                    }
                }
            }
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="SType">SType.</param>
        /// <param name="Type">Type.</param>
        /// <param name="IDObject">IDObject.</param>
        /// <param name="OwnerPlayer">Owner player.</param>
        /// <param name="isKine">If set to <c>true</c> is kinematic.</param>
        /// <param name="Pos">Position.</param>
        /// <param name="Rot">Rotation.</param>
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

        /// <summary>
        /// Sends the message to server.
        /// </summary>
        /// <param name="Command">Command.</param>
        public static void SendMessageToServer(CommandType Command)
        {
            //Encode FlatBuffer
            //Create flatbuffer class
            FlatBufferBuilder fbb = new FlatBufferBuilder(1);

            //Insert login and password
            string UIDBuffer = AvatarName + ";" + AvatarPassword;
            Console.WriteLine("AvatarName: " + UIDBuffer.Split(';')[0]);
            //https://stackoverflow.com/questions/2235683/easiest-way-to-parse-a-comma-delimited-string-to-some-kind-of-object-i-can-loop
            //StringOffset SOUIDBuffer = fbb.CreateString(String.Empty);
            StringOffset SOUIDBuffer = fbb.CreateString(UIDBuffer);

            HazelMessage.HMessage.StartHMessage(fbb);
            HazelMessage.HMessage.AddCommand(fbb, (sbyte)Command);
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
                connection.SendBytes(newArray, SendOption.Reliable);
            }
            if (DEBUG)
            {
                Console.WriteLine("Message sent!");
            }
        }
    }
}