import axios from "axios";

const API_BASE = import.meta.env.DEV ? "http://127.0.0.1:5057/api" : "/api";
const WS_URL = "ws://127.0.0.1:9090";

export interface User {
  id: number;
  username: string;
  isAdmin: boolean;
}

export interface SharedLevel {
  id: number;
  title: string;
  xmlData: string;
  author: { id: number; username: string };
  sharedAt: string;
}

export interface ShareLevelPayload {
  title: string;
  xmlData?: string;
  authorId: number;
}

export interface GameMessage {
  command: string;
  data?: string;
  title?: string;
  cloudId?: number;
  authorId?: number;
  reason?: string;
}

export const api = {
  login: (username: string, password: string) =>
    axios.post<User>(`${API_BASE}/auth/login`, { username, password }),

  listLevels: () => axios.get<SharedLevel[]>(`${API_BASE}/levels`),

  shareLevel: (data: ShareLevelPayload) =>
    axios.post(`${API_BASE}/levels/share`, data),

  updateLevel: (id: number, data: ShareLevelPayload) =>
    axios.put(`${API_BASE}/levels/${id}`, data),

  deleteLevel: (id: number, authorId: number) =>
    axios.delete(`${API_BASE}/levels/${id}?authorId=${authorId}`),
};

class GameSocket {
  private socket: WebSocket | null = null;
  private onStatusChange: (
    status: "connected" | "disconnected" | "refused",
  ) => void = () => {};
  private onMessage: (msg: GameMessage) => void = () => {};
  private currentUser: User | null = null;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private isExplicitlyRefused: boolean = false;

  connect(
    onStatusChange: (status: "connected" | "disconnected" | "refused") => void,
    onMessage: (msg: GameMessage) => void,
  ) {
    this.onStatusChange = onStatusChange;
    this.onMessage = onMessage;
    if (
      this.socket &&
      (this.socket.readyState === WebSocket.OPEN ||
        this.socket.readyState === WebSocket.CONNECTING)
    )
      return;

    this.isExplicitlyRefused = false;
    this.socket = new WebSocket(WS_URL);

    this.socket.onopen = () => {
      this.onStatusChange("connected");
      if (this.currentUser) this.syncUser(this.currentUser);
    };

    this.socket.onmessage = (event) => {
      try {
        const msg = JSON.parse(event.data) as GameMessage;
        if (msg.command === "refuse") {
          this.isExplicitlyRefused = true;
          this.onStatusChange("refused");
        } else {
          this.onMessage(msg);
        }
      } catch (e) {
        console.error("WS parse error", e);
      }
    };

    this.socket.onclose = () => {
      this.socket = null;
      if (this.isExplicitlyRefused) return;
      this.onStatusChange("disconnected");
      if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
      this.reconnectTimer = setTimeout(
        () => this.connect(onStatusChange, onMessage),
        3000,
      );
    };

    this.socket.onerror = () => {
      this.socket?.close();
    };
  }

  disconnect() {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.reconnectTimer = null;
    if (this.socket) {
      this.isExplicitlyRefused = true;
      this.socket.close();
      this.socket = null;
    }
  }

  setCurrentUser(user: User | null) {
    this.currentUser = user;
    this.syncUser(user);
  }

  private syncUser(user: User | null) {
    this.send("set_user", user ? JSON.stringify(user) : null);
  }

  play(lvl: SharedLevel) {
    this.send("play_level", {
      data: lvl.xmlData,
      title: lvl.title,
      cloudId: lvl.id,
      authorId: lvl.author.id,
    });
  }

  edit(lvl: SharedLevel) {
    this.send("edit_level", {
      data: lvl.xmlData,
      title: lvl.title,
      cloudId: lvl.id,
      authorId: lvl.author.id,
    });
  }

  confirmSync(cloudId: number) {
    this.send("sync_success", { cloudId });
  }

  private send(command: string, data: string | object | null) {
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify({ command, data }));
    }
  }
}

export const gameSocket = new GameSocket();
