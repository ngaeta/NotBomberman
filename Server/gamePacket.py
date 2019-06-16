import struct
import time

class Packet:

    ID_PACKETS = 1

    def __init__(self, recived, sender, formats, *args):
        self.time = time.perf_counter()
        self.sender = sender
        self.myIdPacket = Packet.ID_PACKETS
        self.commandId = args[0]
        Packet.ID_PACKETS = Packet.ID_PACKETS + 1

        #self.packet = struct.pack(formats + "I", *args)
        if recived == True:
            self.packet = struct.pack(formats, *args)
            self.myIdPacket = args[len(args)-1]
        else:
            self.packet = struct.pack(formats + "I", *args, self.myIdPacket)

    def getData(self):
        return self.packet

    def getSender(self):
        return self.sender

    def getIdPacket(self):
        return self.myIdPacket

    def getTimePacket(self):
        return self.time