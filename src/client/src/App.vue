<template>
  <div id="app" class="d-flex p-5">
    <div id="call" v-if="cameraUseAllowed">
      <b-container>
        <b-row>
          <b-col><VideoCapture :stream="stream"/></b-col>
          <b-col><VideoReciever/></b-col>
        </b-row>
      </b-container>
    </div>
    <div v-else-if="file != ''">
      <b-container>
        <b-row>
          <b-col><FileStreamer :file="file" /></b-col>
          <b-col><VideoReciever/></b-col>
        </b-row>
      </b-container>
    </div>
    <div v-else>
      <b-container>
        <b-row class="m-4 pb-4">
          <b-button @click="askForCamera" variant="primary">Stream webcam</b-button>
        </b-row>
        <b-row class="m-4 pt-4">
          <input type="file"
            ref="file"
            @change="handleFileUpload()"
            accept="video/webm"
            class="input-file" />
        <b-button  v-on:click="streamFile()" variant="primary">Stream file</b-button>
        </b-row>
      </b-container>
    </div>
    <div>
      <b-alert v-model="showAlert" dismissible variant="danger">{{userMediaError}}</b-alert>
    </div>
  </div>
</template>

<script>
import VideoCapture from './components/VideoCapture.vue'
import VideoReciever from './components/VideoReciever.vue'
import FileStreamer from './components/FileStreamer.vue'

export default {
  name: 'App',
  components: {
    VideoCapture, VideoReciever, FileStreamer
  },
  data() {
    return {
      cameraUseAllowed: false,
      stream: null,
      userMediaError: "",
      showAlert: false,
      roomId: "",
      file: ''
    }
  },
  methods: {
    async askForCamera() {
      const constraints = {
          audio: false,
          video: {
              width: 480, height: 360
          }
      };

      try{
        var stream = await navigator.mediaDevices.getUserMedia(constraints)
        this.$eventBus.$emit('room-joined', JSON.stringify({roomId: this.roomId}));
        this.stream = stream;
        this.cameraUseAllowed = true;
      } catch (error) {
        this.userMediaError = error
        this.showAlert = true;
      }
    },
    handleFileUpload() {
        this.file = this.$refs.file.files[0];
    },
    streamFile() {
      console.log(this.file)
    }
  }
}
</script>