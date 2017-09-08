using System;
using System.Net;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hazel;
using Hazel.Udp;

namespace HazelUDPTestClient
{
		class ClientExample
	{
		static Connection connection;

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

				int i = 0;
				while (i < 1)
                {
                    connection.SendBytes(new byte[] { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,
                    26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67,68, 69, 70,
                        71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100}, SendOption.None);
                }

				Console.WriteLine("Press any key to continue...");

				Console.ReadKey();
				//https://msdn.microsoft.com/it-it/library/471w8d85(v=vs.110).aspx?cs-save-lang=1&cs-lang=cpp#code-snippet-3

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

		private static void DataReceived(object sender, DataReceivedEventArgs args)
		{
			Console.WriteLine("Received (" + string.Join<byte>(", ", args.Bytes) + ") from " + connection.EndPoint.ToString());

			args.Recycle();
		}

		private static void ServerDisconnectHandler(object sender, DisconnectedEventArgs args)
		{
			Connection connection = (Connection)sender;

			Console.WriteLine("Server connection at " + connection.EndPoint + " lost");

			connection = null;

			args.Recycle();
		}
	}
}