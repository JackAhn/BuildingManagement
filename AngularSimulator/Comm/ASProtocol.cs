using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngularSimulator.Comm
{
    class ASProtocol
    {
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private char[] DATA_DELIMETER = { ':' };
        private char[] CRLF = { (char)0x0A, (char)0x0D };
        private char[] HEADER_TAIL = { (char)STX, (char)ETX };

        public delegate void OnParsingCompleted(Object parsered, Object rawpkt, bool isCompleted);
        private OnParsingCompleted completedCallback = null;

        private string lastSendPkt;
        public string LastSendPkt { get => lastSendPkt; set => lastSendPkt = value; }

        public ASProtocol(OnParsingCompleted completedCallback)
        {
            this.completedCallback = completedCallback;
        }

        public bool CheckPacket(String pkt)
        {
            bool isValidPacket = false;

            var realPacket = pkt.Trim(CRLF);

            var sendedPkt = LastSendPkt.Replace("ID", String.Empty).Trim(HEADER_TAIL);

            //ACK 검사
            if(!realPacket.Contains(sendedPkt))
            {
                if (completedCallback != null)
                {
                    completedCallback(pkt.Split(DATA_DELIMETER), pkt, false);
                }

                return false;
            }

            if(realPacket.First() == (char)STX && realPacket.Last() == (char)ETX)
            {
                //valid packet
                isValidPacket = true;

                //패킷이 2개가 붙어서 오는경우도 있음
                char[] seper = { 'M' };
                if (( realPacket.Split(seper).Length - 1) > 0)
                {
                    char[] header = { (char)STX};
                    char[] tail = { (char)ETX };
                    var tmp = realPacket.Trim(header);
                    var pkts = tmp.Split(tail);

                    foreach(string p in pkts)
                    {
                        if (p.Length > 0)
                        {
                            ParsingPacket(p, pkt);
                        }
                    }
                }
                else
                {
                    ParsingPacket(realPacket.Trim(HEADER_TAIL), pkt);
                }
                
            }
            else
            {
                //invalid packet
                isValidPacket = false;
            }

            return isValidPacket;
        }

        public void CheckPacket(Byte[] pkt)
        {

        }

        private void ParsingPacket(String pkt,String rawPkt)
        {
            if (completedCallback != null)
            {
                completedCallback(pkt.Split(DATA_DELIMETER), rawPkt, true);
            }
        }
        
    }
}
