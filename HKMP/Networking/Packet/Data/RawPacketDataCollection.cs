using System.Collections.Generic;

namespace Hkmp.Networking.Packet.Data {
    public class RawPacketDataCollection {
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
        
        public List<IPacketData> DataInstances { get; }

        public RawPacketDataCollection() {
            DataInstances = new List<IPacketData>();
        }
    }
}