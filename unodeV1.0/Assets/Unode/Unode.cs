﻿using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

using WebSocketSharp;

using Debug = UnityEngine.Debug;

public class Unode : MonoBehaviour {
	
	//Websocket
	public string adress;
	public string msg;
	private WebSocket ws;
	private bool run;

	//JSON
	private Dictionary<string,object> dict;

	//Node.exe
	public string program_name;
	public bool   x86;
	public string nodejs_path;
	public string Script_path;
	private Process nodejs_process;
	private ProcessStartInfo info;
	public bool cmd = true;
	public string command;
	public string js;
	public bool windowtype_hidden = false;

	//外部出力
	public bool sound;
	public string file;

	void Awake() {
		if(run = open_nodejs(x86))
			ws = new WebSocket(adress);
	}

	void Start () {
	}
	
	//メインスレッド
	void Update () {
		if (run) {
			ws.OnOpen += (sender, e) => {
					Debug.Log ("ws.OnOpen:");
					ws.Send ("{\"mode\":1}");
			};

			ws.OnMessage += (sender, e) => {
					dict = Json.Deserialize (e.Data) as Dictionary<string,object>;
					try {
							switch ((long)dict ["mode"]) {
							case 1:
									Debug.Log ("Node.js: " + (string)dict ["ver"]);
									ws.Send ("{\"mode\":2" + "}");
									break;
							case 2:
									Debug.Log ("Mode: " + (long)dict ["mode"]);
									break;
							case 3:
									 file = (string) dict ["file"];
									sound =   (bool) dict ["voice"];
									break;
							default:
									Debug.Log ("Error:" + (long)dict ["mode"]);
									break;
							}
					} catch {
							Debug.Log ("Error:parse");
					}
					//Debug.Log("OnMessage:"+Json.Serialize(dict));
			};

			ws.OnClose += (sender, e) => {
					Debug.Log ("OnClosed:" + e.Reason);
			};	



			ws.Connect ();
		} else {
			Debug.Log("Error:Not Run Node.js");
		}
	}

	void OnApplicationQuit() {
		if(ws != null)
			ws.Close();
		kill_nodejs ();
	}
	
	private bool open_nodejs(bool arch){
		if (arch == true) {
			nodejs_path = Application.streamingAssetsPath + "/.node/x86/";
		} else {
			nodejs_path = Application.streamingAssetsPath + "/.node/x64/";
		}
			Script_path = Application.streamingAssetsPath + "/.node/src/";

		info = new System.Diagnostics.ProcessStartInfo();
		info.FileName = nodejs_path + program_name;
		info.WorkingDirectory = Script_path;

		if(cmd){
			info.Arguments = command;
		}else{
			info.Arguments = Script_path + js;
		}

		if(windowtype_hidden){
			info.WindowStyle = ProcessWindowStyle.Hidden;
		}

		try{
			nodejs_process = Process.Start(info);
		}catch(System.ComponentModel.Win32Exception w){
			Debug.Log("Not Found." + w);
			return false;
		}
		return true;
	}

	private void kill_nodejs(){
		nodejs_process.Kill();
	}

	public void message(int mode,string text){
		string msg;

		msg  = "{";
		msg += "\"mode\":" + mode + ",";
		msg += 	text;
		msg += "}";

		Debug.Log (msg);

		ws.Send(msg);
	}
}
