using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Hazel;
using Hazel.Tcp;

namespace HazelTestSuperServer
{
	public class Server
	{
		public static Hashtable clientList = new Hashtable();

		public int portNumber = 4296;

		private int counter = 0;

		public bool Running { get; private set; }

		List<Connection> clients = new List<Connection>();

		public void Start()
		{
			NetworkEndPoint endPoint = new NetworkEndPoint(IPAddress.Any, portNumber);
			ConnectionListener listener = new TcpConnectionListener(endPoint);
			Running = true;
			listener.Start();

			while (Running)
			{
				//Start listening for new connection events
	            listener.NewConnection += delegate(object sender, NewConnectionEventArgs args)
	            {
					//Add client counter
					counter += 1;
					//Connection to List
					clients.Add(args.Connection);
	            };
		  	}

			//Close all
		}

		public void Shutdown()
		{
			if (Running)
			{
				Running = false;
				Console.WriteLine("Shutting down the Hazel Server...");
		   }
	}

	class MainClass
	{
		public static void Main(string[] args)
		{
			Server ServerHazel = new Server();
			ServerHazel.Start();
		}
	}
}
