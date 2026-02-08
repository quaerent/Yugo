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
  const [users, setUsers] = useState<User[]>([]);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const [gameStatus, setGameStatus] = useState<
    "connected" | "disconnected" | "refused"
  >("disconnected");

  const [editingLevelId, setEditingLevelId] = useState<number | null>(null);
  const [editTitle, setEditingTitle] = useState("");

  // User form state
  const [isAddingUser, setIsAddingUser] = useState(false);
  const [newUser, setNewUser] = useState({ username: "", password: "" });

  const loadLevels = useCallback(async () => {
    try {
      const res = await api.listLevels();
      setLevels(res.data);
    } catch {
      console.error("Failed to load levels");
    }
  }, []);

  const loadUsers = useCallback(async () => {
    if (!user || user.id !== 0) return;
    try {
      const res = await api.listUsers(user.id);
      setUsers(res.data);
    } catch {
      console.error("Failed to load users");
    }
  }, [user]);

  const handleCloudSyncFromApp = useCallback(
    async (msg: GameMessage) => {
      const currentUser = userRef.current;
      if (!currentUser || currentUser.id === 0) return;
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
      if (user.id === 0) {
        loadUsers();
        setActiveTab("users");
      } else {
        setActiveTab("levels");
      }
      gameSocket.setCurrentUser(user);
    }
  }, [user, loadLevels, loadUsers]);

  const [activeTab, setActiveTab] = useState<"levels" | "users">("levels");

  useEffect(() => {
    if (!user || user.id === 0) {
      gameSocket.disconnect();
      return;
    }

    gameSocket.connect(
      (status) => setGameStatus(status),
      async (msg) => {
        if (msg.command === "share_level") await handleCloudSyncFromApp(msg);
      },
    );
    return () => {
      gameSocket.disconnect();
    };
  }, [user, handleCloudSyncFromApp]);

  // FULL SYNC: Periodically push entire cloud list to Game for title resolution
  useEffect(() => {
    if (gameStatus !== "connected" || levels.length === 0) return;

    gameSocket.syncCloudList(levels);
    const timer = setInterval(() => gameSocket.syncCloudList(levels), 10000);
    return () => clearInterval(timer);
  }, [gameStatus, levels]);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await api.login(username, password);
      setUser(res.data);
      localStorage.setItem("yugo_user", JSON.stringify(res.data));
    } catch {
      setError("Login failed.");
    }
  };

  const handleLogout = () => {
    setUser(null);
    gameSocket.setCurrentUser(null);
    localStorage.removeItem("yugo_user");
  };

  const deleteLevel = async (id: number) => {
    if (!user || !confirm("Permanently delete?")) return;
    try {
      await api.deleteLevel(id, user.id);
      loadLevels();
    } catch {
      alert("Delete failed.");
    }
  };

  const saveTitle = async (lvl: SharedLevel) => {
    if (!user) return;
    try {
      await api.updateLevel(lvl.id, { title: editTitle, authorId: user.id });
      setEditingLevelId(null);
      gameSocket.updateIdentity(lvl.id, editTitle);
      loadLevels();
    } catch {
      alert("Rename failed.");
    }
  };

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || user.id !== 0) return;
    try {
      await api.createUser(user.id, { ...newUser, isAdmin: false });
      setIsAddingUser(false);
      setNewUser({ username: "", password: "" });
      loadUsers();
    } catch {
      alert("Failed to create user");
    }
  };

  const deleteUser = async (id: number) => {
    if (!user || user.id !== 0 || !confirm("Permanently delete user?")) return;
    try {
      await api.deleteUser(user.id, id);
      loadUsers();
    } catch {
      alert("Delete failed.");
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !user || user.id === 0) return;
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
      <div className="min-h-screen flex items-center justify-center bg-slate-50 font-sans text-slate-900 p-4">
        <form
          onSubmit={handleLogin}
          className="bg-white p-8 rounded-xl shadow-lg border border-slate-200 w-full max-sm:max-w-sm"
        >
          <h1 className="text-2xl font-black mb-6 tracking-tighter text-center text-slate-800 uppercase">
            Yugo Cloud
          </h1>
          {error && (
            <div className="text-red-500 mb-4 text-xs font-bold text-center bg-red-50 py-2 rounded">
              {error}
            </div>
          )}
          <div className="space-y-4">
            <input
              type="text"
              placeholder="Username"
              className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-lg outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <input
              type="password"
              placeholder="Password"
              className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-lg outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
            <button className="w-full py-3 bg-slate-900 hover:bg-black text-white rounded-lg font-black tracking-widest text-xs transition-all shadow-md active:scale-[0.98]">
              SIGN IN
            </button>
          </div>
        </form>
      </div>
    );
  }

  const isAdmin = user.id === 0;

  return (
    <div className="min-h-screen bg-slate-50 font-sans p-4 md:p-8 text-slate-900">
      <header className="max-w-6xl mx-auto flex justify-between items-end mb-8">
        <div>
          <h1 className="text-4xl font-black tracking-tighter uppercase leading-none">
            Yugo <span className="text-blue-600">Cloud</span>
          </h1>
          <div className="flex items-center gap-2 mt-2">
            <span className="text-[10px] font-black uppercase tracking-tighter bg-slate-200 px-1.5 py-0.5 rounded">
              {user.username} {isAdmin && "• ROOT SYSTEM"}
            </span>
            {!isAdmin && (
              <>
                <div
                  className={`w-2 h-2 rounded-full ${gameStatus === "connected" ? "bg-green-500 animate-pulse" : gameStatus === "refused" ? "bg-orange-500" : "bg-red-400"}`}
                ></div>
                <span className="text-[10px] text-slate-400 font-bold uppercase">
                  {gameStatus === "connected" ? "Linked" : "Searching..."}
                </span>
              </>
            )}
          </div>
        </div>
        <div className="flex gap-2">
          {isAdmin ? (
            <div className="bg-white p-1 rounded-lg border border-slate-200 shadow-sm flex">
              <button
                onClick={() => setActiveTab("users")}
                className={`px-4 py-1.5 text-[10px] font-black uppercase rounded ${activeTab === "users" ? "bg-blue-600 text-white shadow-sm" : "text-slate-400 hover:text-slate-600"}`}
              >
                Node Registry
              </button>
              <button
                onClick={() => setActiveTab("levels")}
                className={`px-4 py-1.5 text-[10px] font-black uppercase rounded ${activeTab === "levels" ? "bg-blue-600 text-white shadow-sm" : "text-slate-400 hover:text-slate-600"}`}
              >
                Core Content
              </button>
            </div>
          ) : (
            <div className="bg-white p-1 rounded-lg border border-slate-200 shadow-sm flex">
              <button className="px-4 py-1.5 text-[10px] font-black uppercase rounded bg-blue-600 text-white shadow-sm">
                Community
              </button>
            </div>
          )}
          <button
            onClick={handleLogout}
            className="px-4 py-2 bg-white border border-slate-200 rounded-lg text-[10px] font-black uppercase hover:bg-red-50 hover:text-red-500 transition-all shadow-sm"
          >
            Logout
          </button>
        </div>
      </header>

      <main className="max-w-6xl mx-auto">
        {activeTab === "levels" ? (
          <section className="bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden">
            <div className="px-6 py-4 bg-slate-50/50 border-b border-slate-200 flex justify-between items-center">
              <h2 className="text-xs font-black text-slate-500 uppercase tracking-widest">
                {isAdmin ? "GLOBAL CONTENT REGISTRY" : "COMMUNITY POOL"}
              </h2>
              <div className="flex gap-4 items-center">
                {!isAdmin && (
                  <label className="cursor-pointer text-[10px] font-black text-blue-600 uppercase hover:underline">
                    {isUploading ? "Uploading..." : "Upload XML"}
                    <input
                      type="file"
                      className="hidden"
                      accept=".xml"
                      onChange={handleFileUpload}
                      disabled={isUploading}
                    />
                  </label>
                )}
                <button
                  onClick={loadLevels}
                  className="text-[10px] font-black text-slate-400 uppercase hover:text-slate-600"
                >
                  Refresh
                </button>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/30">
                    <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase">
                      Title & Author
                    </th>
                    <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase text-right">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {levels.length === 0 && (
                    <tr>
                      <td
                        colSpan={2}
                        className="px-6 py-12 text-center text-slate-400 italic text-sm"
                      >
                        Empty registry.
                      </td>
                    </tr>
                  )}
                  {levels.map((lvl: SharedLevel & { authorId?: number }) => {
                    const isAuthor =
                      !isAdmin && user.id === (lvl.author?.id || lvl.authorId);
                    const canManage = isAuthor || isAdmin;
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
                                className="px-2 py-1 border border-blue-300 rounded text-xs outline-none focus:ring-2 focus:ring-blue-500"
                                value={editTitle}
                                onChange={(e) =>
                                  setEditingTitle(e.target.value)
                                }
                                autoFocus
                              />
                              <button
                                onClick={() => saveTitle(lvl)}
                                className="text-[10px] font-black text-green-600"
                              >
                                SAVE
                              </button>
                              <button
                                onClick={() => setEditingLevelId(null)}
                                className="text-[10px] font-black text-slate-400"
                              >
                                CANCEL
                              </button>
                            </div>
                          ) : (
                            <div>
                              <div className="flex items-center gap-2">
                                <span className="font-black text-slate-800 text-sm tracking-tight">
                                  {lvl.title}
                                </span>
                                {canManage && (
                                  <button
                                    onClick={() => {
                                      setEditingLevelId(lvl.id);
                                      setEditingTitle(lvl.title);
                                    }}
                                    className="opacity-0 group-hover:opacity-100 text-[9px] font-black text-blue-500 uppercase"
                                  >
                                    Rename
                                  </button>
                                )}
                              </div>
                              <div className="text-[9px] text-slate-400 uppercase font-black tracking-tighter">
                                By {lvl.author?.username || "System"}
                              </div>
                            </div>
                          )}
                        </td>
                        <td className="px-6 py-4 text-right space-x-2">
                          <div className="flex justify-end gap-1.5">
                            {canManage && (
                              <button
                                onClick={() => deleteLevel(lvl.id)}
                                className="px-3 py-1.5 bg-red-50 text-red-500 hover:bg-red-500 hover:text-white text-[9px] font-black rounded uppercase transition-all"
                              >
                                Delete
                              </button>
                            )}
                            {!isAdmin && (
                              <>
                                <button
                                  onClick={() => gameSocket.play(lvl)}
                                  className="px-4 py-1.5 bg-green-500 hover:bg-green-600 text-white text-[9px] font-black rounded shadow-sm transition-all active:scale-95 disabled:opacity-30"
                                  disabled={gameStatus !== "connected"}
                                >
                                  Play
                                </button>
                                <button
                                  onClick={() => gameSocket.edit(lvl)}
                                  className="px-4 py-1.5 bg-slate-800 hover:bg-black text-white text-[9px] font-black rounded shadow-sm transition-all active:scale-95 disabled:opacity-30"
                                  disabled={gameStatus !== "connected"}
                                >
                                  Edit
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </section>
        ) : (
          <section className="space-y-6">
            <div className="flex justify-between items-center bg-white p-6 rounded-2xl shadow-sm border border-slate-200">
              <div>
                <h2 className="text-xl font-black uppercase tracking-tighter">
                  Identity Management
                </h2>
                <p className="text-xs text-slate-400 font-bold uppercase tracking-widest mt-1">
                  Authorized Nodes: {users.length}
                </p>
              </div>
              <button
                onClick={() => setIsAddingUser(!isAddingUser)}
                className="px-6 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-[10px] font-black uppercase rounded-lg shadow-md transition-all active:scale-95"
              >
                Add Node
              </button>
            </div>

            {isAddingUser && (
              <form
                onSubmit={handleCreateUser}
                className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 space-y-4 animate-in fade-in slide-in-from-top-4"
              >
                <h3 className="text-slate-400 text-[10px] font-black uppercase tracking-widest mb-4">
                  Identity Protocol
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <input
                    type="text"
                    placeholder="Username"
                    className="bg-slate-50 border border-slate-200 text-slate-800 text-sm rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 outline-none"
                    value={newUser.username}
                    onChange={(e) =>
                      setNewUser({ ...newUser, username: e.target.value })
                    }
                    required
                  />
                  <input
                    type="text"
                    placeholder="Password"
                    className="bg-slate-50 border border-slate-200 text-slate-800 text-sm rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 outline-none"
                    value={newUser.password}
                    onChange={(e) =>
                      setNewUser({ ...newUser, password: e.target.value })
                    }
                    required
                  />
                </div>
                <div className="flex justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => setIsAddingUser(false)}
                    className="px-4 py-2 text-slate-400 text-[10px] font-black uppercase hover:text-slate-600 transition-colors"
                  >
                    Abort
                  </button>
                  <button
                    type="submit"
                    className="px-6 py-2 bg-slate-900 text-white text-[10px] font-black uppercase rounded-lg shadow-md active:scale-95 transition-all"
                  >
                    Initialize
                  </button>
                </div>
              </form>
            )}

            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden">
              <table className="w-full text-left">
                <thead className="bg-slate-50/50">
                  <tr>
                    <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase">
                      Registered Users
                    </th>
                    <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase">
                      Role
                    </th>
                    <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase text-right">
                      Operations
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {users.map((u) => (
                    <tr
                      key={u.id}
                      className="hover:bg-slate-50/50 transition-all"
                    >
                      <td className="px-6 py-4">
                        <div className="font-black text-slate-800 text-sm">
                          {u.username}
                        </div>
                        <div className="text-[9px] text-slate-400 font-bold uppercase tracking-tighter">
                          Node ID: {u.id}
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className="px-2 py-0.5 text-[9px] font-black rounded uppercase bg-slate-100 text-slate-400">
                          EndUser
                        </span>
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex justify-end gap-2">
                          {u.username !== "admin" ? (
                            <>
                              <button
                                onClick={async () => {
                                  const newPass = prompt("Enter new password:");
                                  if (newPass === null) return;
                                  await api.updateUser(user.id, u.id, {
                                    username: u.username,
                                    password: newPass,
                                    isAdmin: false,
                                  });
                                  alert("Protocol Updated.");
                                }}
                                className="text-[10px] font-black text-blue-600 uppercase hover:underline"
                              >
                                Reset
                              </button>
                              <button
                                onClick={() => deleteUser(u.id)}
                                className="text-[10px] font-black text-red-500 uppercase hover:underline"
                              >
                                Terminate
                              </button>
                            </>
                          ) : (
                            <span className="text-[9px] text-slate-300 font-bold uppercase italic">
                              Root Restricted
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}
      </main>
    </div>
  );
}

export default App;
