using System.Collections.Generic;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Non-generic version of packet data collection.
/// </summary>
public class RawPacketDataCollection {
    /// <summary>
    /// Whether this collection should be treated as reliable.
    /// </summary>
    public bool IsReliable {
        get {
            foreach (var dataInstance in DataInstances) {
                if (dataInstance.IsReliable) {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Whether the data in this collection should be dropped if newer data is already received.
    /// </summary>
    public bool DropReliableDataIfNewerExists {
        get {
            foreach (var dataInstance in DataInstances) {
                if (dataInstance.DropReliableDataIfNewerExists) {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// A list of packet data instances in this collection.
    /// </summary>
    public List<IPacketData> DataInstances { get; }

    /// <summary>
    /// Construct the data collection.
    /// </summary>
    protected RawPacketDataCollection() {
        DataInstances = new List<IPacketData>();
    }
}
