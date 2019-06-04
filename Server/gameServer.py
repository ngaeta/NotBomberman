import socket
import sys
import time
import numpy
import struct
import select
import os

from gamePlayer import GamePlayer
from gameBomb import GameBomb
from gamePacket import Packet
from ColliisonsDetection.CollisionMng import CollisionMng

COMMAND_JOIN = 0
COMMAND_SHOOT_BOMB = 1
COMMAND_SPAWN_BOMB = 2
COMMAND_SPAWN_PLAYER = 3
COMMAND_SEND_VELOCITY = 4
COMMAND_RECEIVE_POS = 5
COMMAND_BOMB_TIMER = 6
COMMAND_PLAYER_DIE = 7
COMMAND_JOIN_ACK = 8
COMMAND_ACK = 9
COMMAND_ALIVE = 10

COLOR_BLU = 0
COLOR_ORANGE = 1
COLOR_YELLOW = 2
COLOR_RED = 3

OBJ_PLAYER = 1
OBJ_BOMB = 0

HZ = 1.0 / 10

class GameServer:

    COUNT = 0

    def __init__(self, address, port):
        self.name_server = "NotBomberman"
        self.max_players = 4
        self.address = address
        self.port = port
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.setblocking(False)
        self.socket.bind((self.address, self.port))
        self.commands_table = {}
        self.commands_table[COMMAND_JOIN] = self.join
        self.commands_table[COMMAND_SHOOT_BOMB] = self.shoot_bomb
        self.commands_table[COMMAND_SEND_VELOCITY] = self.send_velocity
        self.commands_table[COMMAND_ALIVE] = self.alive
        self.players = {}
        self.bombs = []
        self.next_wait = HZ
        self.send_all_queue = []
        self.send_target = []
        self.color_avaiable = {COLOR_BLU : True, COLOR_ORANGE : True, COLOR_YELLOW : True, COLOR_RED : True}
        self.start_position = [[-4.05,0.53,4.2],[4.07,0.53,4.2],[-4.05,0.53,-4.4],[4.07,0.53,-4.39]]
        self.start_position_avaiable = {0 : True, 1 : True, 2 : True, 3 : True}
        self.deltaTime = 0
        self.timer_game = 60
        self.packet_arrived = {} #Keys IDPACKET_ARRIVED - Values MY_ACK
        self.delete_packet_arrived = []
        GameBomb.server = self
        print('Server started!')

    def ClearDeadPlayer(self, who):
        player = self.players[who]
        self.color_avaiable[player.color_player] = True
        for index, avaiable in self.start_position_avaiable.items():
            x = self.start_position[index][0]
            y = self.start_position[index][1]
            z = self.start_position[index][2]
            xx = player.start_pos[0]
            yy = player.start_pos[1]
            zz = player.start_pos[2]
            if x == xx and y == yy and z == zz:
                self.start_position_avaiable[index] = True
                break
        del(self.players[who])

    def tick_players(self, now):
        dead_clients = []
        for player in self.players.values():
            if now - player.last_packet_timestamp > 20:
                dead_clients.append(player.address)
            else:
                player.tick()

        for dead_client in dead_clients:
            print('{} ({}) is dead'.format(self.players[dead_client].name, dead_client))
            #del(self.players[dead_client])
            self.ClearDeadPlayer(dead_client)
            
    
        # send enqueued packets to all clients
        for packet in self.send_all_queue:
            for player in self.players.values():
                self.socket.sendto(packet.getData(), player.address)
        self.send_all_queue = []

    def tick_server(self):
        #wait when join all player need refactoring
        if len(self.players) < self.max_players:
            return

        self.timer_game -= self.deltaTime / 2
        if self.timer_game <= 0:
            print("Game finishied")

    def tick_bomb(self):
        dead_bombs = []
        for bomb in self.bombs:
            if bomb.dead == True:
                dead_bombs.append(bomb)
            else:
                bomb.tick(self.deltaTime/2)

        for dead_bomb in dead_bombs:
            if dead_bomb in self.bombs:
                self.bombs.remove(dead_bomb)

    def tick(self):
        before = time.perf_counter()
        rlist, _, _ = select.select([self.socket], [], [], self.next_wait)
        after = time.perf_counter()

        self.next_wait -= after - before
        self.deltaTime = after - before
        if self.socket not in rlist or self.next_wait <= 0:
            self.tick_players(time.perf_counter())
            self.tick_bomb()
            CollisionMng.update()
            self.tick_server()
            self.next_wait = HZ

        if self.socket not in rlist:
            return

        try:
            self.oldPacket, self.sender = self.socket.recvfrom(64)
        except socket.error:
            return

        # check packet size
        if len(self.oldPacket) <= 0:
            return

        command = self.oldPacket[0]

        #if you are not a player i refuse all packet except the JOIN COMMAND
        if self.sender not in self.players:
            if command != COMMAND_JOIN:
                return

        # dispatcher
        if command in self.commands_table:
            format = self.GetFormatPacket(command, self.oldPacket)
            args_packet = self.GetArgsPacket(command, self.oldPacket)
            if format == 0 or args_packet == 0:
                print("Wrong Format or Args Unpack")
                return
            self.packet = Packet(True, self.sender, format, *args_packet)

            #Resend packet if i have the same packet else check the dead packet
            #for remove the packet
            client_idPacket = (self.sender, self.packet.getIdPacket())
            if client_idPacket in self.packet_arrived.keys():
                self.socket.sendto(self.packet_arrived[client_idPacket].getData(), self.packet_arrived[client_idPacket].getSender())
                return
            else:
                if client_idPacket in self.packet_arrived:
                    if time.perf_counter() - self.packet_arrived[client_idPacket].getTimePacket() > 15:
                        self.delete_packet_arrived.append(client_idPacket)

            self.commands_table[command](self.packet)

        #send packets enqueued for a player
        for player in self.players.values():
            for packet in player.send_queue:
                self.socket.sendto(packet.getData(), player.address)
            player.send_queue = []


        #Delete packet_arrived
        for old_client_idPacket in self.delete_packet_arrived:
            if old_client_idPacket in self.packet_arrived.keys():
                del(self.packet_arrived[old_client_idPacket])
                print("Old Packet deleted!")
            




    #Methods for create a NewPacket from OldPacket - START
    def GetFormatPacket(self, command, packets):
        if command == COMMAND_JOIN:
            len_name = packets[1]
            return "=BB" + str(len_name) + "sI"
        elif command == COMMAND_SEND_VELOCITY:
            return "=BfffI"
        elif command == COMMAND_SHOOT_BOMB:
            return "=BfffI"
        elif command == COMMAND_ALIVE:
            return "=BI"
        else:
            return 0

    def GetArgsPacket(self, command, packet):
        if command == COMMAND_JOIN:
            len_name = packet[1]
            _,__,name,id_packet = struct.unpack("=BB" + str(len_name) + "sI", packet)
            return [COMMAND_JOIN, len_name, name, id_packet]
        elif command == COMMAND_SEND_VELOCITY:
            _,x,y,z,id_packet = struct.unpack("=BfffI", packet)
            return [COMMAND_SEND_VELOCITY, x, y, z, id_packet]
        elif command == COMMAND_SHOOT_BOMB:
            _,x,y,z,id_packet = struct.unpack("=BfffI", packet)
            return [COMMAND_SHOOT_BOMB, x, y, z, id_packet]
        elif command == COMMAND_ALIVE:
            _,id_packet = struct.unpack("=BI", packet)
            return [COMMAND_ALIVE, id_packet]
        else:
            return 0
            
    ############################################### - END


    def GetColorAvaiable(self):
        for color, avaiable in self.color_avaiable.items():
            if avaiable == True:
                self.color_avaiable[color] = False
                return color

    def GetPositionAvaiable(self):
        for index, avaiable in self.start_position_avaiable.items():
            if avaiable == True:
                self.start_position_avaiable[index] = False
                x = self.start_position[index][0]
                y = self.start_position[index][1]
                z = self.start_position[index][2]
                print(x,y,z)
                return [x,y,z]


    def join(self, packet_info):
        #only 4 Player can join
        #TODO: send ack for no login
        if len(self.players) >= self.max_players:
            return

        #check multiple join
        if self.sender in self.players:
            print("Multiple Join {}".format(self.sender))
            return

        len_name = packet_info.getData()[1]

        #refuse join if name is > of 10 chars
        if len_name > 10:
            print("Refuse Join beacuse len is > 10")
            return

        _,__,name,id_packet = struct.unpack("=BB" + str(len_name) + "sI", packet_info.getData())
        name = bytes(name).decode("utf-8")

        #check multiple name
        #for player in self.players.values():
        #    if player.name == name:
        #        print("Double Name can't Join")
        #        return

        start_pos = self.GetPositionAvaiable()
        color = self.GetColorAvaiable()
        player = GamePlayer(name, self.sender, start_pos[0], start_pos[1], start_pos[2])
        player.color_player = color

        spawn_player = Packet(False, self.sender, "=BIfffB{}s".format(len(player.name)), COMMAND_SPAWN_PLAYER, player.id, start_pos[0],start_pos[1],start_pos[2], player.color_player, player.name.encode())
        for player_spawn in self.players.values():
            self.socket.sendto(spawn_player.getData(), player_spawn.address)
            spawn_player2 = Packet(False, self.sender, "=BIfffB{}s".format(len(player_spawn.name)), COMMAND_SPAWN_PLAYER, player_spawn.id, player_spawn.x, player_spawn.y, player_spawn.z, player_spawn.color_player, player_spawn.name.encode())
            self.socket.sendto(spawn_player2.getData(), self.sender)

        self.players[self.sender] = player

        ack_packet = Packet(False, self.sender, "=BBIfffB", COMMAND_JOIN_ACK, 1, player.id, start_pos[0],start_pos[1],start_pos[2], color)

        player.send_queue.append(ack_packet)
        self.packet_arrived[(self.sender, id_packet)] = ack_packet

        print('{} ({}) joined in the room ({} of {} players)'.format(name, self.sender, len(self.players), self.max_players))

    def shoot_bomb(self, packet_info):
        
        if not self.sender in self.players:
            return

        _,x,y,z,id_packet = struct.unpack("=BfffI", packet_info.getData())
        bomb = GameBomb(self.sender, x, y, z)
        self.bombs.append(bomb)


        ack_packet = Packet(True, self.sender, "=BI", COMMAND_ACK, id_packet)
        spawn_packet = Packet(False, self.sender, "=BIfffff", COMMAND_SPAWN_BOMB, bomb.id, x, y, z, bomb.radius, bomb.timer_dead)
        self.packet_arrived[(self.sender, id_packet)] = ack_packet


        player = self.players[self.sender]
        player.send_queue.append(ack_packet)
        player.send_queue.append(spawn_packet)

        for player_bomb in self.players.values():
            if player.address != player_bomb.address:
                self.socket.sendto(spawn_packet.getData(), player_bomb.address)

        print("Drop Bomb from {}".format(player.name))

    def send_velocity(self, packet_info):

        if not self.sender in self.players:
            return

        _,x,y,z,id_packet = struct.unpack("=BfffI", packet_info.getData())
        #print("command:     {}".format(_))
        #print("X:           {}".format(x))
        #print("Y:           {}".format(y))
        #print("Z:           {}".format(z))
        #print("IdPacket:    {}".format(id_packet))

        player = self.players[self.sender]
        player.x += x * (self.deltaTime / 2)
        player.y += y * (self.deltaTime / 2)
        player.z += z * (self.deltaTime / 2)

        ack_pos_packet = Packet(False, self.sender, "=BIfff", COMMAND_RECEIVE_POS, player.id, player.x, player.y, player.z)

        self.send_all_queue.append(ack_pos_packet)

    def alive(self, packet_info):
        if not self.sender in self.players:
            return

        _, id_packet = struct.unpack("=BI", packet_info.getData())
        player = self.players[self.sender]
        player.last_packet_timestamp = time.perf_counter()
        print("Alive from {}".format(player.name))



    #def spawn_obj(self, packet_info):
    def deltaTimeFunc(self):
        tick_server = time.perf_counter()
        last_time = time.perf_counter()
        if tick_server - last_time >= 1000:
            last_time = tick_server
        
        self.deltaTime = ((tick_server - last_time) / 1000.0)

        


        


game_server = GameServer('127.0.0.1', 9999)
#game_server = GameServer('192.168.1.220', 9999)
#game_server = GameServer('192.168.3.194', 9999)
#game_server = GameServer('192.168.20.80', 9999)

while True:
    game_server.tick()
    game_server.tick_server()