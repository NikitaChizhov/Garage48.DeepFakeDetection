
export default {
  install (Vue) {
    const connection = new WebSocket("ws://localhost:5000/ws");

    connection.onopen = (event) => {
      console.log(event)
      console.log("Successfully connected to the websocket server")
    }

    connection.onclose = (event) => {
      console.log(event)
      console.log("Connected to the websocket server is closed")
    }

    const eventBus = new Vue();
    Vue.prototype.$eventBus = eventBus;

    connection.onmessage = (event) => {
      if(typeof event.data === 'string' || event.data instanceof String){
        eventBus.$emit('evaluated', event.data)
      }
      else{
        eventBus.$emit('video-recieved', event)
      }
    };

    eventBus.$on('video-recorded', (data) => {
      connection.send(data)
    })

    eventBus.$on('room-joined', (data) => {
      connection.send(data)
    })
  }
}