import { useState, useEffect, useRef, useCallback } from "react";
import { api, gameSocket } from "./api";
import type { User, SharedLevel, GameMessage } from "./api";

function App() {
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem("yugo_user");
    return saved ? JSON.parse(saved) : null;
  });

  const userRef = useRef<User | null>(user);
  const [levels, setLevels] = useState<SharedLevel[]>([]);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const [gameStatus, setGameStatus] = useState<
    "connected" | "disconnected" | "refused"
  >("disconnected");

  const [editingLevelId, setEditingLevelId] = useState<number | null>(null);
  const [editTitle, setEditingTitle] = useState("");

  const loadLevels = useCallback(async () => {
    try {
      const res = await api.listLevels();
      setLevels(res.data);
    } catch {
      console.error("Failed to load levels");
    }
  }, []);

  const handleCloudSyncFromApp = useCallback(
    async (msg: GameMessage) => {
      const currentUser = userRef.current;
      if (!currentUser) return;
      try {
        const payload = {
          title: msg.title || "Untitled",
          xmlData: msg.data,
          authorId: currentUser.id,
        };
        let res;
        if (msg.cloudId && msg.cloudId !== -1) {
          res = await api.updateLevel(msg.cloudId, payload);
        } else {
          res = await api.shareLevel(payload);
        }
        gameSocket.confirmSync(res.data.id);
        loadLevels();
      } catch {
        console.error("Cloud Sync failed");
      }
    },
    [loadLevels],
  );

  useEffect(() => {
    userRef.current = user;
    if (user) {
      loadLevels();
      gameSocket.setCurrentUser(user);
    }
  }, [user, loadLevels]);

  useEffect(() => {
    if (user) gameSocket.setCurrentUser(user);

    gameSocket.connect(
      (status) => setGameStatus(status),
      async (msg) => {
        if (msg.command === "share_level") {
          await handleCloudSyncFromApp(msg);
        }
      },
    );

    return () => {
      gameSocket.disconnect();
    };
  }, [user, handleCloudSyncFromApp]);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await api.login(username, password);
      setUser(res.data);
      gameSocket.setCurrentUser(res.data);
      localStorage.setItem("yugo_user", JSON.stringify(res.data));
    } catch {
      setError("Login failed. Try admin/admin.");
    }
  };

  const handleLogout = () => {
    setUser(null);
    gameSocket.setCurrentUser(null);
    localStorage.removeItem("yugo_user");
  };

  const deleteLevel = async (id: number) => {
    if (!user || !confirm("Are you sure?")) return;
    try {
      await api.deleteLevel(id, user.id);
      loadLevels();
    } catch {
      alert("Delete failed.");
    }
  };

  const startEditing = (lvl: SharedLevel) => {
    setEditingLevelId(lvl.id);
    setEditingTitle(lvl.title);
  };

  const saveTitle = async (lvl: SharedLevel) => {
    if (!user) return;
    try {
      await api.updateLevel(lvl.id, { title: editTitle, authorId: user.id });
      setEditingLevelId(null);
      loadLevels();
    } catch {
      alert("Rename failed.");
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !user) return;
    setIsUploading(true);
    const formData = new FormData();
    formData.append("file", file);
    formData.append("title", file.name.replace(".xml", ""));
    formData.append("authorId", user.id.toString());
    try {
      await fetch("http://127.0.0.1:5057/api/levels/upload-file", {
        method: "POST",
        body: formData,
      });
      loadLevels();
    } catch {
      alert("Upload failed");
    } finally {
      setIsUploading(false);
    }
  };

  if (!user) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 font-sans text-slate-900">
        <form
          onSubmit={handleLogin}
          className="bg-white p-8 rounded-xl shadow-lg border border-slate-200 w-96"
        >
          <h1 className="text-2xl font-bold mb-6 tracking-tight text-center text-slate-800">
            YUGO CLOUD
          </h1>
          {error && (
            <div className="text-red-500 mb-4 text-sm font-medium text-center">
              {error}
            </div>
          )}
          <div className="space-y-4">
            <input
              type="text"
              placeholder="Username"
              className="w-full px-4 py-2 bg-slate-100 border-none rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <input
              type="password"
              placeholder="Password"
              className="w-full px-4 py-2 bg-slate-100 border-none rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
            <button className="w-full py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold transition-all shadow-md active:scale-[0.98]">
              SIGN IN
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 font-sans p-8 text-slate-900">
      <header className="max-w-5xl mx-auto flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-black tracking-tighter text-slate-900">
            YUGO <span className="text-blue-600">CLOUD</span>
          </h1>
          <div className="flex items-center gap-2">
            <p className="text-slate-500 text-xs font-bold uppercase tracking-widest">
              Connected as {user.username}
            </p>
            <div
              className={`w-2 h-2 rounded-full ${
                gameStatus === "connected"
                  ? "bg-green-500 animate-pulse"
                  : gameStatus === "refused"
                    ? "bg-orange-500"
                    : "bg-red-400"
              }`}
            ></div>
            <span className="text-[10px] text-slate-400 font-bold uppercase">
              {gameStatus === "connected"
                ? "App Linked"
                : gameStatus === "refused"
                  ? "App Busy"
                  : "Searching for App..."}
            </span>
          </div>
        </div>
        <div className="flex items-center gap-4">
          <label className="cursor-pointer px-4 py-2 bg-white border border-slate-200 rounded-lg text-xs font-bold hover:bg-slate-50 transition-colors shadow-sm">
            {isUploading ? "UPLOADING..." : "UPLOAD .XML FILE"}
            <input
              type="file"
              className="hidden"
              accept=".xml"
              onChange={handleFileUpload}
              disabled={isUploading}
            />
          </label>
          <button
            onClick={handleLogout}
            className="text-slate-400 hover:text-red-500 text-xs font-bold transition-colors"
          >
            LOGOUT
          </button>
        </div>
      </header>

      <main className="max-w-5xl mx-auto space-y-6">
        <section className="bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden">
          <div className="px-6 py-4 bg-slate-50/50 border-b border-slate-200 flex justify-between items-center">
            <h2 className="text-sm font-black text-slate-700 uppercase tracking-wider">
              Community Levels
            </h2>
            <button
              onClick={loadLevels}
              className="text-xs font-bold text-blue-600 hover:text-blue-800"
            >
              REFRESH LIST
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50/30">
                  <th className="px-6 py-3 text-xs font-bold text-slate-400 uppercase">
                    Level Details
                  </th>
                  <th className="px-6 py-3 text-xs font-bold text-slate-400 uppercase text-right">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {levels.length === 0 && (
                  <tr>
                    <td
                      colSpan={2}
                      className="px-6 py-12 text-center text-slate-400 italic"
                    >
                      No levels found.
                    </td>
                  </tr>
                )}
                {levels.map((lvl: SharedLevel & { authorId?: number }) => {
                  const isAuthor =
                    user.id === lvl.author?.id || lvl.authorId === user.id;
                  const isEditing = editingLevelId === lvl.id;

                  return (
                    <tr
                      key={lvl.id}
                      className="group hover:bg-slate-50/50 transition-all"
                    >
                      <td className="px-6 py-4">
                        {isEditing ? (
                          <div className="flex items-center gap-2">
                            <input
                              className="px-2 py-1 border border-blue-300 rounded text-sm outline-none focus:ring-2 focus:ring-blue-500"
                              value={editTitle}
                              onChange={(e) => setEditingTitle(e.target.value)}
                              autoFocus
                            />
                            <button
                              onClick={() => saveTitle(lvl)}
                              className="text-xs font-bold text-green-600"
                            >
                              SAVE
                            </button>
                            <button
                              onClick={() => setEditingLevelId(null)}
                              className="text-xs font-bold text-slate-400"
                            >
                              CANCEL
                            </button>
                          </div>
                        ) : (
                          <div>
                            <div className="flex items-center gap-2">
                              <span className="font-bold text-slate-800">
                                {lvl.title}
                              </span>
                              {isAuthor && (
                                <button
                                  onClick={() => startEditing(lvl)}
                                  className="opacity-0 group-hover:opacity-100 text-[10px] text-blue-500 font-bold hover:underline"
                                >
                                  RENAME
                                </button>
                              )}
                            </div>
                            <div className="text-[10px] text-slate-400 uppercase font-bold tracking-tighter">
                              By {lvl.author?.username || "Unknown"}
                            </div>
                          </div>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right space-x-2">
                        <div className="flex justify-end gap-2">
                          {isAuthor && (
                            <button
                              onClick={() => deleteLevel(lvl.id)}
                              className="px-3 py-1 bg-red-50 text-red-500 hover:bg-red-500 hover:text-white text-[10px] font-black rounded transition-all"
                            >
                              DELETE
                            </button>
                          )}
                          <button
                            onClick={() => gameSocket.play(lvl)}
                            className="px-4 py-1.5 bg-green-500 hover:bg-green-600 text-white text-xs font-black rounded shadow-sm transition-all active:scale-95 disabled:opacity-50"
                            disabled={gameStatus !== "connected"}
                          >
                            PLAY
                          </button>
                          <button
                            onClick={() => gameSocket.edit(lvl)}
                            className="px-4 py-1.5 bg-slate-800 hover:bg-black text-white text-xs font-black rounded shadow-sm transition-all active:scale-95 disabled:opacity-50"
                            disabled={gameStatus !== "connected"}
                          >
                            EDIT
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      </main>
    </div>
  );
}

export default App;
