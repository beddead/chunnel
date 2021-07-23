namespace chunnel.Model.Config
{
  /// <summary>
  /// TCP connection mode
  /// </summary>
  internal enum TcpMode
  {
    /// <summary>
    /// Connection to specific IP address and TCP port
    /// </summary>
    Client,

    /// <summary>
    /// Listening for incoming connection on specific IP address and TCP port
    /// </summary>
    Server
  }
}