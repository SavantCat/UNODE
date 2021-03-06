var cluster = require('cluster');
var numCPUs = require('os').cpus().length;

var child_process = require("child_process");
var ws = require('websocket.io');
var msgpack = require('msgpack-js');

var children = {};

function sendTOunity(client,message){
    try{
        var bytedata = msgpack.encode(message);
        client.send(bytedata,{binary:true});
	
        //console.log("Nodejs -> Unity:",bytedata);
    }catch(e){
        console.log("Error:",e);
    }
}


if (cluster.isMaster) {
  for (var i = 0; i < numCPUs; i++) {
    cluster.fork();
  }
 
  cluster.on('exit', function(worker, code, signal) {
    console.log("worker("+worker.id+").exit " + worker.process.pid);
  });
  cluster.on('online', function(worker) {
    console.log("worker("+worker.id+").online " + worker.process.pid);
  });
  cluster.on('listening', function(worker, address) {
    console.log("worker("+worker.id+").listening " + address.address + ":" + address.port);
  });
 
} else {
    
    var server = ws.listen(8080,function () {
	console.log("Websocket Server start");
      }
    );    

    server.on('connection', function(client) {
	//console.log('connection start:'+client.port);
	
	// クライアントからのメッセージ受信イベントを処理
	client.on('message', function(request) {
	    var data = msgpack.decode(request);
	    //console.log("Unity -> Nodejs:",data);
    
	    //--------------Main area--------------
	    switch (data.mode) {
		case "connect":
		    var message = {
			mode : 'connected',
			ver  : 'v0.10.28'
		    }
		    sendTOunity(client,message);
		    break;
		case "child":
		    console.log("Run:" + data.name);
		    if (data.regist != null) {
			var child = child_process.fork("./"+data.js);
			children[data.name] = {
			    "child" : child,
			    "cilent": client
			}
			children[data.name].child.on("message", function(msg) {
			   console.log(msg);
			   sendTOunity(children[msg.name].cilent,msg); 
			});
		    }else{
			children[data.name].child.send(data.options);
		    }
		    break;
		case "transform":
		    var message = {
			mode : 'transform',
		    }		    
		    sendTOunity(client,message);
		    console.log(data)
		    break;
		case "echo":
		    var message = {
			mode : data.mode,
			text : data.text
		    }
		    sendTOunity(client,message);
		    break;
		case "exit":
		    process.exit();
		    break;
	    }
	    //--------------------------------------
	});
     
	// クライアントが切断したときの処理
	client.on('disconnect', function(){
	    console.log('connection disconnect');
	});
     
	// 通信がクローズしたときの処理
	client.on('close', function(){
	    console.log('connection close');
	});
     
	// エラーが発生した場合
	client.on('error', function(err){
	    console.log(err);
	    console.log(err.stack);
	});
    });
}
