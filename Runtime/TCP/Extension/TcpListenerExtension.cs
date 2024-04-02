using System.Net.Sockets;
using System.Net;

namespace HRYooba.Network.Tcp
{
    /// <summary>
    /// TcpListenerの拡張メソッド
    /// </summary>
    internal static class TcpListenerExtension
    {
        /// <summary>
        /// IPアドレスのstringを取得
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        internal static string ToIPAddressString(this TcpListener self) => ((IPEndPoint)self.Server.LocalEndPoint).Address.ToString();

        /// <summary>
        /// ポート番号取得
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        internal static int ToPort(this TcpListener self) => ((IPEndPoint)self.Server.LocalEndPoint).Port;
    }
}