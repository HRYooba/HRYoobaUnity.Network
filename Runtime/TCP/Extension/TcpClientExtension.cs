using System.Net.Sockets;
using System.Net;

namespace HRYooba.Network.Tcp
{
    /// <summary>
    /// TcpClientの拡張メソッド
    /// </summary>
    internal static class TcpClientExtension
    {
        /// <summary>
        /// IPアドレスのstringを取得
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        internal static string ToIPAddressString(this TcpClient self) => ((IPEndPoint)self.Client.RemoteEndPoint).Address.ToString();

        /// <summary>
        /// ポート番号取得
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        internal static int ToPort(this TcpClient self) => ((IPEndPoint)self.Client.RemoteEndPoint).Port;
    }
}