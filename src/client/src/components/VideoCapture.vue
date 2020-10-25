<template>
  <div>
    <video ref="video" playsinline autoplay width="480px" video="360px"></video>
  </div>
</template>

<script>
export default {
  name: 'VideoCapture',
  props: {
    stream: MediaStream
  },
  data() {
    return {
      mediaRecorder: null
    }
  },
  mounted() {
    this.$refs.video.srcObject = this.stream;

    var options = {mimeType: 'video/webm;codecs=vp9'};
    this.mediaRecorder = new MediaRecorder(this.stream, options);

    this.mediaRecorder.ondataavailable = event => {
        let blob = event.data;
        this.$eventBus.$emit('video-recorded', blob);
      };

    this.mediaRecorder.start(1000);
  }
}
</script>