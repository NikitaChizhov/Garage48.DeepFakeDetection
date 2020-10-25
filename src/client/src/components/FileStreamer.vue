<template>
  <div>
    <b-container>
        <b-row class="m-4 pb-4">
            <video ref="video" playsinline autoplay width="480px" video="360px"></video>
        </b-row>
        <b-row class="m-4 pb-4">
            <b-button @click="show" kind="primary">Start stream</b-button>
        </b-row>
    </b-container>
  </div>
</template>

<script>
export default {
  name: 'FileStreamer',
  props: {
    file: File
  },
  data() {
    return {
      mediaRecorder: null
    }
  },
  methods: {
      show() {
        var options = {mimeType: 'video/webm;codecs=vp9'};
        const stream = this.$refs.video.captureStream();

        this.mediaRecorder = new MediaRecorder(stream, options);
        this.mediaRecorder.ondataavailable = event => {
            let blob = event.data;
            this.$eventBus.$emit('video-recorded', blob);
        };

        this.mediaRecorder.start(1000);
      }
  },
  mounted() {
    this.$refs.video.src = URL.createObjectURL(this.file);
    this.$eventBus.$emit("room-joined", JSON.stringify({roomId: this.file.name}))
  }
}
</script>