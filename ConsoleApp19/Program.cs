using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class Program
{
    private static List<TcpClient> clients = new List<TcpClient>();
    private static object lockObject = new object();

    static void Main(string[] args)
    {
        TcpListener server = null;
        try
        {
            // Establece el servidor para escuchar en el puerto 13002
            Int32 port = 13002;
            IPAddress localAddr = IPAddress.Parse("172.20.11.9");
            server = new TcpListener(localAddr, port);

            // Inicia el servidor
            server.Start();

            // Loop infinito para aceptar conexiones
            while (true)
            {
                Console.WriteLine("Esperando una conexión... ");

                // Acepta una conexión de cliente
                TcpClient client = server.AcceptTcpClient();
                lock (lockObject)
                {
                    clients.Add(client);
                }
                Console.WriteLine("Conectado!");

                // Crea un nuevo thread para manejar al cliente
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            // Detiene el servidor
            server.Stop();
        }

        Console.WriteLine("\nPresiona ENTER para continuar...");
        Console.Read();
    }

    static void HandleClient(TcpClient client)
    {
        byte[] bytes = new byte[256];
        NetworkStream stream = client.GetStream();

        // Crear un nuevo thread para leer mensajes del cliente
        Thread readThread = new Thread(() => ReadFromClient(client, stream));
        readThread.Start();

        // Loop para enviar mensajes al cliente
        while (true)
        {
            // Lee el mensaje a enviar desde la consola
            string messageToSend = Console.ReadLine();

            if (messageToSend.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            byte[] msg = Encoding.ASCII.GetBytes(messageToSend);

            // Envía los datos a todos los clientes conectados
            BroadcastMessage(msg);
            Console.WriteLine("Enviado: {0}", messageToSend);
        }

        // Cierra la conexión con el cliente
        client.Close();
    }

    static void ReadFromClient(TcpClient client, NetworkStream stream)
    {
        byte[] bytes = new byte[256];
        int i;

        // Loop para recibir todos los datos enviados por el cliente
        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
        {
            // Traduce los datos recibidos a una cadena de texto
            string data = Encoding.ASCII.GetString(bytes, 0, i);
            Console.WriteLine("Recibido del cliente: {0}", data);

            byte[] msg = Encoding.ASCII.GetBytes(data);

            // Retransmitir el mensaje a todos los clientes
            BroadcastMessage(msg);
        }

        lock (lockObject)
        {
            clients.Remove(client);
        }
    }

    static void BroadcastMessage(byte[] msg)
    {
        lock (lockObject)
        {
            foreach (var cl in clients)
            {
                NetworkStream clientStream = cl.GetStream();
                clientStream.Write(msg, 0, msg.Length);
            }
        }
    }
}
