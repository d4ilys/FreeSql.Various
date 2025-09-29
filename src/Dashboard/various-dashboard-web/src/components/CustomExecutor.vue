<script lang="jsx" setup>
import {apiUrl} from '../tools.js';
import {useNotification, useMessage, useDialog, useModal} from 'naive-ui'
import {taskQueue} from '../task-queue.js';
import {h, ref} from "vue";

const notification = useNotification();
const message = useMessage()
const dialog = useDialog()
const modal = useModal()
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
}).then(res => res.text()).then(data => {
  executors.value = JSON.parse(data);
})

taskQueue.on('taskReady', (task, onComplete) => {
  const data = {...task.item};
  data.onComplete = onComplete;
  task.handler(data);
});

function exec(item) {
  const url = apiUrl('/executor');
  const source = new EventSource(`${url}?${new URLSearchParams({
    id: item.id,
    group: item.group
  })}`);

  source.onmessage = (e) => {
    const item = JSON.parse(e.data);
    const calls = handleBackendCalls(item);
    taskQueue.enqueue({
      handler: calls.handler,
      item: item
    })
  };

  source.onerror = (e) => {
    source.close();
  }
}

function handleBackendCalls(item) {
  return processingBackendCalls.find(call => call.type === item.type)
}

//#region 处理后端需要前端做的事情！

const handleDialog = (item) => {
  dialog[item.body.type]({
    title: item.body.title,
    content: item.body.content,
    positiveText: '确定',
    onPositiveClick: (done) => {
      item.onComplete()
    },
    onMaskClick: () => {
      item.onComplete();
    },
    onClose: () => {
      item.onComplete();
    },
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
  item.onComplete();
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
  item.onComplete();
}

processingBackendCalls.push({
  type: "HideLoading",
  handler: handleHideLoading,
});

const handleMessage = (item) => {
  message[item.body.type](item.body.message, {
    duration: item.body.duration
  })
  item.onComplete();
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
  item.onComplete();
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
  if (item.body.contentStyle) {
    for (const header in item.body.contentStyle) {
      dialogContentStyle[header] = item.body.contentStyle[header];
    }
  }

  if (item.body.content.length > 100) {
    style.width = "770px"
  }

  dialog.warning({
    title: item.body.title,
    content: () => h('p', {
      innerHTML: item.body.content
    }),
    positiveText: "执行",
    contentStyle: dialogContentStyle,
    style: style,
    negativeText: "取消",
    draggable: true,
    onClose: () => {
      item.onComplete();
    },
    onMaskClick: () => {
      item.onComplete();
    },
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
        dialogWaring(data);
        item.onComplete();
      }).catch(e => {
        dialogWaring(e);
        item.onComplete();
      });
    },
    onNegativeClick: () => {
      item.onComplete();
    }
  });
};

processingBackendCalls.push({
  type: "AfterConfirmRequest",
  handler: handleAfterConfirmRequest,
})

const handleModalFromRequest = (item) => {
  const formValue = reactive({});
  const rules = {};
  const formRef = ref(null);
  const components = item.body.Components;
  for (let component of components) {
    rules[component.Name] = component.Rules
    formValue[component.Name] = component.DefaultValue;
  }
  const m = modal.create({
    title: item.body.Title,
    preset: 'card',
    maskClosable: false,
    style: {
      marginTop: dialogMarginTop,
      width: "500px"
    },
    onClose: () => {
      item.onComplete();
    },
    onNegativeClick: () => {
      item.onComplete();
    },
    onMaskClick: () => {
      item.onComplete();
    },
    footer: () =>
        <div style="text-align: right">
          <NButton onClick={close} size="small">
            取消
          </NButton>
          &nbsp;&nbsp;&nbsp;&nbsp;
          <NButton onClick={commit} type="success" size="small">
            提交
          </NButton>
        </div>,
    content: () => (
        <n-form model={formValue} rules={rules} size="small" ref={formRef}>
          <br/>
          {item.body.Components.map(component =>
              renderFormComponent(component, formValue)
          )}
        </n-form>
    )
  })

  function close() {
    m.destroy()
    item.onComplete();
  }

  function commit() {
    formRef.value?.validate((errors) => {
      if (!errors) {
        // 执行请求
        const requestRouter = item.body.Router;
        const requestJsonBody = JSON.stringify(formValue);
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
          dialogWaring(data);
          item.onComplete();
        }).catch(e => {
          dialogWaring(e);
          item.onComplete();
        });
      } else {
        message.error('必填项不能为空')
        item.onComplete();
      }
    })
  }
};

// 渲染表单组件的函数
const renderFormComponent = (component, formValue) => {
  const commonProps = {
    path: component.Name,
    label: component.Label,
    key: component.Name // 使用name作为key更稳定，避免index带来的问题
  };

  switch (component.Type) {
    case 0:
      return (
          <n-form-item {...commonProps}>
            <n-input
                v-model:value={formValue[component.Name]}
                placeholder={`请输入${component.Label}`}
            />
          </n-form-item>
      );

    case 1:
      return (
          <n-form-item {...commonProps}>
            <n-input
                type="textarea"
                v-model:value={formValue[component.Name]}
                placeholder={`请输入${component.Label}`}
                rows={component.rows || 3}
            />
          </n-form-item>
      );

    case 2:
      return (
          <n-form-item {...commonProps}>
            <n-select
                placeholder={`请选择${component.Label}`}
                v-model:value={formValue[component.Name]}
                options={component.Options || []}
            />
          </n-form-item>
      );

    default:
      return null;
  }
};

processingBackendCalls.push({
  type: "ModalFromRequest",
  handler: handleModalFromRequest,
})

function dialogWaring(message) {
  dialog.warning({
    title: '通知',
    style: {
      marginTop: "250px"
    },
    content: message,
    positiveText: '确定',
    draggable: true,
  })
}

const handleAlert = (item) => {
  dialog.info({
    title: item.body.title,
    style: {
      marginTop: dialogMarginTop
    },
    content: item.body.content,
    positiveText: '确定',
    onPositiveClick: (done) => {
      item.onComplete()
    },
    onClose: () => {
      item.onComplete();
    },
    draggable: true
  })
};
processingBackendCalls.push({
  type: "Alert",
  handler: handleAlert,
})

const handleOpenUrl = (item) => {
  window.open(item.body, '_blank');
  item.onComplete();
};

processingBackendCalls.push({
  type: "OpenUrl",
  handler: handleOpenUrl,
})

//#endregion

</script>

<template>
  <template v-for="item in executors" :key="item.group">
    <n-divider title-placement="left">
      {{ item.group }}
    </n-divider>
    <n-space>
      <n-button :type="randomButtonType()" v-for="b in item.executors" :key="b.id" @click="exec(b)">
        {{
          b.title
        }}
      </n-button>
    </n-space>
  </template>


</template>

<style scoped>

</style>
