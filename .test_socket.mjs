import net from 'net';

const client = new net.Socket();

client.connect(8080, '127.0.0.1', () => {
  console.log('Connected. Sending say_hello...');
  const req = JSON.stringify({
    jsonrpc: '2.0',
    method: 'say_hello',
    params: {},
    id: 'req1'
  });
  client.write(req);
});

let data = '';
client.on('data', (chunk) => {
  data += chunk.toString();
});

client.on('error', (err) => {
  console.log('Socket error:', err.message);
  process.exit(1);
});

setTimeout(() => {
  console.log('RESPONSE:', data.slice(0, 800) || '(nothing)');
  client.destroy();
  process.exit(0);
}, 6000);
