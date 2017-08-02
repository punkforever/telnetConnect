namespace Telnet
{
    using System;
    using System.Runtime.InteropServices;

    [InterfaceType(ComInterfaceType.InterfaceIsDual), ComVisible(true), Guid("86AC1F2D-B32A-4457-AC28-CB5423B181D9")]
    public interface IConnector
    {
        [DispId(1)]
        int connect(string ipaddr, int portnr);
        [DispId(2)]
        int disconnect();
        [DispId(3)]
        int communicate();
        [DispId(4)]
        string buffer { get; set; }
        [DispId(5)]
        int timeout { get; set; }
        [DispId(6)]
        string lasterrortext { get; }
        [DispId(7)]
        bool makelog { get; set; }
    }
}

