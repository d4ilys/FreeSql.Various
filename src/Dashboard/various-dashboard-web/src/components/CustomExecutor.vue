<script setup>
import {apiUrl} from '../tools.js';
import {useNotification, useMessage, useDialog} from 'naive-ui'

const notification = useNotification();
const message = useMessage()
const dialog = useDialog()
let msgReactive = null
const loadingRef = ref("")
const executors = ref([]);
const processingBackendCalls = [];
const buttonType = ["primary", "info", "success", "warning", "error"]

function randomButtonType() {
  return buttonType[Math.floor(Math.random() * buttonType.length)]
}

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
    handleBackendCalls(JSON.parse(e.data));
  };

  source.onerror = (e) => {
    source.close();
  }
}

function handleBackendCalls(item) {
  return processingBackendCalls.find(call => call.type === item.type)?.handler(item)
}

//#region 处理后端需要前端做的事情！

const handleDialog = (item) => {
  dialog[item.body.type]({
    title: item.body.title,
    content: item.body.content,
    positiveText: '确定',
    draggable: true
  })
}

processingBackendCalls.push({
  type: "Dialog",
  handler: handleDialog,
});

const handleShowLoading = (item) => {
  if (msgReactive) {
    loadingRef.value = item.body
    msgReactive.content = `${loadingRef.value}`
  } else {
    msgReactive = message.create(`${loadingRef.value}`, {
      type: "loading",
      duration: 0
    })
  }
}

processingBackendCalls.push({
  type: "ShowLoading",
  handler: handleShowLoading,
});

const handleHideLoading = (item) => {
  if (msgReactive) {
    msgReactive.destroy();
    msgReactive = null
  }
}

processingBackendCalls.push({
  type: "HideLoading",
  handler: handleHideLoading,
});

const handleMessage = (item) => {
  message[item.body.type](item.body.message, {
    duration: item.body.duration
  })
}

processingBackendCalls.push({
  type: "Message",
  handler: handleMessage,
});

const handleNotification = (item) => {
  notification[item.body.type]({
    content: item.body.title,
    meta: item.body.message,
    duration: item.body.duration
  })
}

processingBackendCalls.push({
  type: "Notification",
  handler: handleNotification,
});

const dialogMarginTop = "200px"

const handleAfterConfirmRequest = (item) => {
  const style = {
    marginTop: dialogMarginTop
  };
  const dialogContentStyle = {};
  console.log(item.body.contentStyle)
  if (item.body.contentStyle) {
    for (const header in item.body.contentStyle) {
      dialogContentStyle[header] = item.body.contentStyle[header];
    }
  }


  if (item.body.content.length > 100) {
    style.width = "700px"
  }

  dialog.warning({
    title: item.body.title,
    content: item.body.content,
    positiveText: "执行",
    contentStyle: dialogContentStyle,
    style: style,
    negativeText: "取消",
    draggable: true,
    onPositiveClick: () => {
      // 执行请求
      const requestRouter = item.body.router;
      const requestJsonBody = item.body.jsonBody;
      const requestHeaders = {
        'Content-Type': 'application/json'
      };

      if (item.body.headers) {
        for (const header in item.body.headers) {
          requestHeaders[header] = item.body.headers[header];
        }
      }

      fetch(apiUrl(requestRouter), {
        method: 'POST',
        headers: requestHeaders,
        body: requestJsonBody
      }).then(res => res.text()).then(data => {
        dialog.success({
          title: '提示',
          style: {
            marginTop: dialogMarginTop
          },
          content: data,
          positiveText: '确定'
        })
      })
    },
    onNegativeClick: () => {
      message.info("取消执行");
    }
  });
};

processingBackendCalls.push({
  type: "AfterConfirmRequest",
  handler: handleAfterConfirmRequest,
})

const handleAlert = (item) => {
  console.log(item.body)
  dialog.info({
    title: item.body.title,
    style: {
      marginTop: dialogMarginTop
    },
    content: item.body.content,
    positiveText: '确定',
    draggable: true
  })
};
processingBackendCalls.push({
  type: "Alert",
  handler: handleAlert,
})

const handleOpenUrl = (item) => {
  window.open(item.body, '_blank');
};

processingBackendCalls.push({
  type: "OpenUrl",
  handler: handleOpenUrl,
})

//#endregion

</script>

<template>

  <n-space>
    <n-button :type="randomButtonType()" v-for="item in executors" :key="item" @click="exec(item)">{{
        item.title
      }}
    </n-button>
  </n-space>

</template>

<style scoped>

</style>
