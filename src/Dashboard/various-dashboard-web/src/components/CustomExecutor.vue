<script setup>
import {apiUrl} from '../tools.js';
import {useNotification, useMessage} from 'naive-ui'

const notification = useNotification();
const message = useMessage()
let msgReactive = null
const loadingRef = ref("")
const executors = ref([]);

fetch(apiUrl('/getExecutors'), {
  method: 'GET',
}).then(res => res.json()).then(data => {
  executors.value = data;
})

function exec(item) {
  const url = apiUrl('/executor');
  const source = new EventSource(`${url}?${new URLSearchParams({
    id: item.id,
  })}`);

  source.onmessage = (e) => {
    show(JSON.parse(e.data));
  };

  source.onerror = (e) => {
    source.close();
  }
}

function show(item) {
  if (item.type === "Notification") {
    console.log(notification)
    notification[item.body.type]({
      content: item.body.title,
      meta: item.body.message,
      duration: item.body.duration
    })
  } else if (item.type === "Message") {
    message[item.body.type](item.body.message, {
      duration: item.body.duration
    })
  } else if (item.type === "ShowLoading") {
    if (msgReactive) {
      loadingRef.value = item.body
      msgReactive.content = `${loadingRef.value}`
    } else {
      msgReactive = message.create(`${loadingRef.value}`, {
        type: "loading",
        duration: 0
      })
    }

  } else if (item.type === "HideLoading") {
    if (msgReactive) {
      msgReactive.destroy();
      msgReactive = null
    }
  }
}

</script>

<template>

  <n-button-group>
    <n-button v-for="item in executors" :key="item" ghost @click="exec(item)">{{ item.title }}</n-button>
  </n-button-group>
</template>

<style scoped>

</style>
