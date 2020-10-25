<template>
  <div class="hello">
    <b-container>
        <b-row class="m-4 pb-4">
          <video ref="video" playsinline autoplay width="480px" video="360px"></video>
        </b-row>
        <b-row class="m-4 pb-4">
          <span> Estimated {{evalResult.prediction == "?" ? "?" : Math.round(evalResult.prediction * 100) / 100}} probability of this being a fake.</span>
        </b-row>
    </b-container>
  </div>
</template>

<script>
export default {
  name: 'VideoCapture',
  data() {
    return {
      recordedChunks: [],
      evalResult: {prediction: "?"}
    }
  },
  mounted() {
    var mediaSource = new MediaSource();
    var url = URL.createObjectURL(mediaSource);
    var video = this.$refs.video;
    video.src = url

    var sourceBuffer = null;
    mediaSource.addEventListener("sourceopen", () => {
        sourceBuffer = mediaSource.addSourceBuffer("video/webm;codecs=vp9");
        sourceBuffer.addEventListener("update-end", appendToSourceBuffer);
    });

    var appendToSourceBuffer = () => {
        if (mediaSource.readyState === "open" && sourceBuffer && sourceBuffer.updating === false)
        {
            sourceBuffer.appendBuffer(this.recordedChunks.shift());
        }
        if (video.buffered.length && video.buffered.end(0) - video.buffered.start(0) > 60)
        {
            sourceBuffer.remove(0, video.buffered.end(0) - 60)
        }
    }

    this.$eventBus.$on('video-recieved', (event) => {
      event.data.slice(0, event.data.size, 'video/webm;codecs=vp9').arrayBuffer().then(buffer => {
        this.recordedChunks.push(buffer)
      }).then(() => appendToSourceBuffer());
    })

    this.$eventBus.$on('evaluated', (event) => {
      this.evalResult = JSON.parse(event);
    })
  },
}
</script>