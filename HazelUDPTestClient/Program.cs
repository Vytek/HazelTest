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

				connection.SendBytes(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, SendOption.Reliable);

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